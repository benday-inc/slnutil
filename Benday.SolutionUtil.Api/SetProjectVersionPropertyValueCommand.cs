
using Benday.CommandsFramework;

using System.Diagnostics;
using System.Xml;

using System.Xml.Linq;

namespace Benday.SolutionUtil.Api;


[Command(Name = Constants.CommandArgumentNameSetVersion,
        Description = "Set the assembly and nuget package version property value on a project.")]
public class SetProjectVersionPropertyValueCommand : SynchronousCommand
{

    public SetProjectVersionPropertyValueCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameSolutionPath).AsNotRequired()
            .WithDescription("Solution to examine. If this value is not supplied, the tool searches for a sln file automatically.");

        args.AddString(Constants.ArgumentNameProjectName).AsRequired()
            .WithDescription("Project name to update.");

        args.AddBoolean(Constants.ArgumentNameIncrement)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Increment the minor value.")
            .WithDefaultValue(false);

        args.AddString(Constants.ArgumentNameValue)
            .AsNotRequired()
            .WithDescription("Value for the property.");

        return args;
    }


    protected override void OnExecute()
    {
        string? solutionPath;

        if (Arguments.HasValue(Constants.ArgumentNameSolutionPath) == true)
        {
            solutionPath = Arguments[Constants.ArgumentNameSolutionPath].Value;
            ProjectUtilities.AssertFileExists(solutionPath, Constants.ArgumentNameSolutionPath);
        }
        else
        {
            solutionPath = ProjectUtilities.FindSolution();
        }

        var foundSolution = false;

        if (string.IsNullOrEmpty(solutionPath) == true)
        {
            throw new KnownException("No solution found");
        }
        else
        {
            foundSolution = true;
            WriteLine($"Using solution: {solutionPath}");
        }

        if (foundSolution == true)
        {
            var propertyValue = Arguments.GetStringValue(Constants.ArgumentNameValue
                );
            var hasPropertyValue = Arguments.HasValue(Constants.ArgumentNameValue
                );
            var increment = Arguments.GetBooleanValue(Constants.ArgumentNameIncrement);

            if (hasPropertyValue == false &&
                increment == false)
            {
                throw new KnownException("You must specify a value to set or specify the /increment flag.");
            }

            var projectName = Arguments.GetStringValue(Constants.ArgumentNameProjectName);
            var projectPath = GetProjectPath(solutionPath, projectName);

            if (increment == true)
            {
                var version = ProjectUtilities.GetProjectVersion(projectPath);

                if (string.IsNullOrWhiteSpace(version) == true)
                {
                    version = "1.0.0";
                }
                else
                {
                    version = ProjectUtilities.IncrementVersion(version);
                }

                SetPropertyValue(
                    solutionPath,
                    new List<string> { projectPath }, "Version",
                    version);

                SetPropertyValue(
                    solutionPath,
                    new List<string> { projectPath }, "AssemblyVersion",
                    version);
            }
            else
            {
                SetPropertyValue(
                    solutionPath,
                    new List<string> { projectPath },
                    "Version",
                    propertyValue);

                SetPropertyValue(
                    solutionPath,
                    new List<string> { projectPath },
                    "AssemblyVersion",
                    propertyValue);
            }
        }
    }

    private string GetProjectPath(string solutionPath, string projectName)
    {
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = "dotnet";

        startInfo.ArgumentList.Add("sln");
        startInfo.ArgumentList.Add(solutionPath);
        startInfo.ArgumentList.Add("list");
        startInfo.RedirectStandardOutput = true;

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Process start returned null");

        process.WaitForExit();

        var projects = new List<string>();

        var line = process.StandardOutput.ReadLine();

        var lineNumber = 0;

        while (line != null)
        {
            if (lineNumber == 0 || lineNumber == 1)
            {
                // skip header
            }
            else
            {
                projects.Add(line);
            }

            lineNumber++;
            line = process.StandardOutput.ReadLine();
        }

        var match = projects.FirstOrDefault(x =>
            x.Contains(projectName, StringComparison.CurrentCultureIgnoreCase) ||
            x.Contains($"{projectName}.csproj", StringComparison.CurrentCultureIgnoreCase) ||
            x.Contains($"{projectName}.vsproj", StringComparison.CurrentCultureIgnoreCase) ||
            x.Contains($"{projectName}.fsproj", StringComparison.CurrentCultureIgnoreCase));

        if (match == null)
        {
            throw new KnownException($"Could not find project '{projectName}' in solution '{solutionPath}'.");
        }
        else
        {
            return match;
        }
    }

    private void SetPropertyValue(string solutionPath, List<string> projects, string propertyName, string propertyValue)
    {
        var dir = Path.GetDirectoryName(solutionPath);

        if (!Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Problem finding directory for solution '{solutionPath}'.");
        }

        foreach (var project in projects)
        {
            SetPropertyValue(dir, project, propertyName, propertyValue);
        }
    }

    private void SetPropertyValue(string dir, string project, string propertyName, string propertyValue)
    {
        var pathToProjectFile = Path.Combine(dir, project);

        if (!File.Exists(pathToProjectFile))
        {
            throw new InvalidOperationException($"Could not find project file '{pathToProjectFile}'");
        }

        var doc = XDocument.Load(pathToProjectFile);

        var root = doc.Root;

        if (root == null || root.Name.LocalName != "Project")
        {
            throw new InvalidOperationException($"Project file does not contain a project definition");
        }

        var result = ProjectUtilities.SetProjectPropertyElement(pathToProjectFile, root, propertyName, propertyValue);

        if (result == null || result.ValueChanged == false)
        {
            // no change
            WriteLine($"No change to project '{pathToProjectFile}'.");
        }
        else
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            using var writer = XmlWriter.Create(pathToProjectFile, settings);

            doc.Save(writer);

            WriteLine($"Updated project '{pathToProjectFile}'.");
        }
    }
}
