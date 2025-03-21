
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

using Benday.CommandsFramework;
using Benday.Common;
using Benday.XmlUtilities;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameSetPackageVersion,
    IsAsync = false,
    Description = "Changes NuGet package references in a C# project file to a new value.")]
public class SetPackageVersionCommand : SynchronousCommand
{
    public SetPackageVersionCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameSolutionPath)
            .AsNotRequired()
            .WithDescription("Solution file to use");

        args.AddBoolean(Constants.ArgumentNamePreview)
            .AsNotRequired().AllowEmptyValue().WithDescription("Preview changes only");

        args.AddString(Constants.ArgumentNamePackageNameFilter)
            .AsNotRequired()
            .WithDescription("Filter package by name. If package name starts with this value, it gets updated.")
            .WithDefaultValue(string.Empty);

        args.AddString(Constants.ArgumentNamePackageVersion)
            .AsRequired()
            .WithDescription("Package version to reference");            

        return args;
    }

    private string _SolutionPath = string.Empty;
    private string _SolutionFolder = string.Empty;

    protected override void OnExecute()
    {
        var targetVersionValue = Arguments.GetStringValue(Constants.ArgumentNamePackageVersion);

        targetVersionValue = targetVersionValue.Trim();

        WriteLine($"Setting package version to '{targetVersionValue}'.");

        if (Arguments.HasValue(Constants.ArgumentNameSolutionPath) == true)
        {
            _SolutionPath = Arguments[Constants.ArgumentNameSolutionPath].Value;
            ProjectUtilities.AssertFileExists(_SolutionPath, Constants.ArgumentNameSolutionPath);
        }
        else
        {
            _SolutionPath = ProjectUtilities.FindSolution().SafeToString();
        }

        var foundSolution = false;

        if (string.IsNullOrEmpty(_SolutionPath) == true)
        {
            WriteLine("no solution found");
        }
        else
        {
            foundSolution = true;

            _SolutionFolder = new FileInfo(_SolutionPath).DirectoryName ??
                throw new InvalidOperationException($"Solution path does not have a non-null directory name.");

            WriteLine($"Solution:        {_SolutionPath}");
            WriteLine($"Solution Folder: {_SolutionFolder}");
        }

        if (foundSolution == true)
        {
            var projectsAsString = GetSolutionProjects();

            if (projectsAsString.Length == 0)
            {
                WriteLine("No projects found.");
            }
            else
            {
                var projects = ParseProjects(projectsAsString);
                UpdateReferences(projects, targetVersionValue);
            }
        }
    }

    private void UpdateReferences(List<string> projectPaths, string targetVersionValue)
    {
        var previewMode = Arguments.GetBooleanValue(Constants.ArgumentNamePreview);

        var filter = Arguments.GetStringValue(Constants.ArgumentNamePackageNameFilter);

        bool doFilter;

        if (string.IsNullOrWhiteSpace(filter) == false)
        {
            doFilter = true;
            WriteLine($"Filtering by package name starting with '{filter}'.");
        }
        else
        {
            doFilter = false;
            WriteLine("Not filtering by package name.");
        }

        foreach (var projectPath in projectPaths)
        {
            var hasChanges = false;
            var wroteProjectName = false;

            var projectPathAbsolute = Path.Combine(_SolutionFolder, projectPath);

            string text = File.ReadAllText(projectPathAbsolute);

            var doc = XDocument.Parse(text);

            var packageRefs = doc.Descendants("PackageReference");

            foreach (var packageRef in packageRefs)
            {
                var include = packageRef.AttributeValue("Include");                

                if (doFilter == true && include.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase) == false)
                {
                    // skip this one
                    continue;
                }

                var version = packageRef.AttributeValue("Version");                

                if (previewMode == true)
                {
                    if (wroteProjectName == false)
                    {
                        WriteLine($"{projectPath}");
                        wroteProjectName = true;
                    }

                    WriteLine($"\t{include} - '{version}' -> '{targetVersionValue}'");
                }
                else
                {
                    packageRef.SetAttributeValue("Version", targetVersionValue);

                    hasChanges = true;

                    if (wroteProjectName == false)
                    {
                        WriteLine($"{projectPath}");
                        wroteProjectName = true;
                    }

                    WriteLine($"\t{include} - '{version}' -> '{targetVersionValue}'");
                }
            }

            if (hasChanges == true && Arguments.GetBooleanValue(Constants.ArgumentNamePreview) == false)
            {                
                var xml = doc.ToString();
                File.WriteAllText(projectPathAbsolute, xml);
                WriteLine($"Saved changes to {projectPathAbsolute}.");
            }
        }
    }

    private List<string> ParseProjects(string projectsAsString)
    {
        var returnValues = new List<string>();

        using var reader = new StringReader(projectsAsString);

        var line = reader.ReadLine();

        while (line != null)
        {
            returnValues.Add(line);

            line = reader.ReadLine();
        }

        return returnValues;
    }

    internal string GetSolutionProjects()
    {
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = "dotnet";

        startInfo.ArgumentList.Add("sln");
        startInfo.ArgumentList.Add(_SolutionPath);
        startInfo.ArgumentList.Add("list");
        startInfo.RedirectStandardOutput = true;

        var process = Process.Start(startInfo) ??
            throw new InvalidOperationException($"Process.Start() returned null.");

        process.WaitForExit();

        var output = new StringBuilder();

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
                output.AppendLine(line);
            }

            lineNumber++;
            line = process.StandardOutput.ReadLine();
        }

        return output.ToString();
    }
}