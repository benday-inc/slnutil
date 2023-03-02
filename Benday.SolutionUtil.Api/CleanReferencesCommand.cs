
using Benday.CommandsFramework;
using Benday.XmlUtilities;

using System;
using System.Diagnostics;
using System.Linq;

using System.Text;
using System.Xml.Linq;

namespace Benday.SolutionUtil.Api;



[Command(Name = Constants.CommandArgumentNameCleanReferences, IsAsync = false)]
public class CleanReferencesCommand : SynchronousCommand
{

    public CleanReferencesCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
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

        return args;
    }

    private string _SolutionPath;
    private string _SolutionFolder;

    protected override void OnExecute()
    {        
        if (Arguments.ContainsKey(Constants.ArgumentNameSolutionPath) == true)
        {
            _SolutionPath = Arguments[Constants.ArgumentNameSolutionPath].Value;
            ProjectUtilities.AssertFileExists(_SolutionPath, Constants.ArgumentNameSolutionPath);
        }
        else
        {
            _SolutionPath = ProjectUtilities.FindSolution();
        }

        var foundSolution = false;

        if (string.IsNullOrEmpty(_SolutionPath) == true)
        {
            WriteLine("no solution found");
        }
        else
        {
            foundSolution = true;

            _SolutionFolder = new FileInfo(_SolutionPath).DirectoryName;

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
                CleanProjects(projects);
            }
        }
    }

    private void CleanProjects(List<string> projectPaths)
    {
        var wroteProjectHeader = false;

        foreach (var projectPath in projectPaths)
        {
            wroteProjectHeader = false;

            var projectPathAbsolute = Path.Combine(_SolutionFolder, projectPath);

            string text = File.ReadAllText(projectPathAbsolute);

            var doc = XDocument.Parse(text);

            var packageRefs = doc.Descendants("PackageReference");

            var foundJunk = false;
            var removeThese = new List<XElement>();

            foreach (var packageRef in packageRefs)
            {
                var include = packageRef.AttributeValue("Include");
                var version = packageRef.AttributeValue("Version");
                var hasChildren = packageRef.HasElements;

                if (hasChildren == true)
                {
                    if (wroteProjectHeader == false)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"* PROJECT: {projectPath}");
                        wroteProjectHeader = true;
                    }

                    Console.WriteLine($"\t{include} - {version} - has junk: {hasChildren}");
                    foundJunk = true;
                    removeThese.AddRange(packageRef.Elements());
                }
            }

            if (foundJunk == true && Arguments.ContainsKey(Constants.ArgumentNamePreview) == false)
            {
                removeThese.ForEach(x => x.Remove());
                var xml = doc.ToString();
                File.WriteAllText(projectPathAbsolute, xml);
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

        var process = Process.Start(startInfo);

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