using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Benday.CommandsFramework;
using Benday.XmlUtilities;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameSetFrameworkVersion,
        Description = "Set the target framework version on all projects.")]
public class SetFrameworkVersionCommand : SynchronousCommand
{

    public SetFrameworkVersionCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameSolutionPath).AsNotRequired()
            .WithDescription("Solution to examine. If this value is not supplied, the tool searches for a sln file automatically.");

        args.AddString(Constants.ArgumentNameFrameworkVersion).AsRequired()
            .WithDescription("Framework version to set projects to.");

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
            var frameworkVersion = Arguments.GetStringValue(Constants.ArgumentNameFrameworkVersion);

            UpdateFrameworkVersions(solutionPath, frameworkVersion);
        }
    }

    private void UpdateFrameworkVersions(string solutionPath, string frameworkVersion)
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

        UpdateFrameworkVersions(solutionPath, projects, frameworkVersion);
    }

    private void UpdateFrameworkVersions(string solutionPath, List<string> projects, string frameworkVersion)
    {
        var dir = Path.GetDirectoryName(solutionPath);

        if (!Directory.Exists(dir))
        {
            throw new InvalidOperationException($"Problem finding directory for solution '{solutionPath}'.");
        }

        foreach (var project in projects)
        {
            UpdateFrameworkVersion(dir, project, frameworkVersion);
        }
    }

    private void UpdateFrameworkVersion(string dir, string project, string frameworkVersion)
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

        var targetFrameworkElement =
            ProjectUtilities.FindTargetFrameworkElement(pathToProjectFile, root, false);

        if (targetFrameworkElement == null)
        {
            // no target framework element found
            WriteLine($"Could not locate target framework for project '{pathToProjectFile}'. Skipping.");
        }
        else
        {
            var currentValue = targetFrameworkElement.Value;

            var newFrameworkVersion = Utilities.GetFrameworkVersion(currentValue, frameworkVersion);

            targetFrameworkElement.Value = newFrameworkVersion;

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            using var writer = XmlWriter.Create(pathToProjectFile, settings);

            doc.Save(writer);

            WriteLine($"Updated project '{pathToProjectFile}' framework version from '{currentValue}' to '{newFrameworkVersion}'.");
        }
    }    
}
