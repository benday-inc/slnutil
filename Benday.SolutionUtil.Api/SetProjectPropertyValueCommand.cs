using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Benday.CommandsFramework;
using Benday.XmlUtilities;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameSetProjectProperty,
        Description = "Set a project property value on all projects.")]
public class SetProjectPropertyValueCommand : SynchronousCommand
{

    public SetProjectPropertyValueCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameSolutionPath).AsNotRequired()
            .WithDescription("Solution to examine. If this value is not supplied, the tool searches for a sln file automatically.");

        args.AddString(Constants.ArgumentNamePropertyName).AsRequired()
            .WithDescription("Name of the property to set.");

        args.AddString(Constants.ArgumentNamePropertyValue).AsRequired()
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
            var propertyName = Arguments.GetStringValue(Constants.ArgumentNamePropertyName);
            var propertyValue = Arguments.GetStringValue(Constants.ArgumentNamePropertyValue);

            SetPropertyValueInProjects(solutionPath, propertyName, propertyValue);
        }
    }

    private void SetPropertyValueInProjects(string solutionPath, string propertyName, string propertyValue)
    {
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = "dotnet";

        startInfo.ArgumentList.Add("sln");
        startInfo.ArgumentList.Add(solutionPath);
        startInfo.ArgumentList.Add("list");
        startInfo.RedirectStandardOutput = true;

        var projects = new List<string>();

        using (var process = Process.Start(startInfo) ?? 
            throw new InvalidOperationException("Process start returned null"))
        {
            process.WaitForExit();


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
        }

        SetPropertyValue(solutionPath, projects, propertyName, propertyValue);
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

        var doc = XDocument.Parse(File.ReadAllText(pathToProjectFile));

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

            writer.Close();

            WriteLine($"Updated project '{pathToProjectFile}'.");
        }
    }
}
