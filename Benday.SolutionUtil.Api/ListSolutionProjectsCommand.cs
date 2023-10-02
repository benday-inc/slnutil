using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using System.Xml;

using Benday.CommandsFramework;
using Benday.Common;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameListSolutionProjects,
        Description = "Gets list of projects in a solution.")]
public class ListSolutionProjectsCommand : SynchronousCommand
{

    public ListSolutionProjectsCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameSolutionPath).AsNotRequired()
            .WithDescription("Solution to examine. If this value is not supplied, the tool searches for a sln file automatically.");

        args.AddBoolean(Constants.ArgumentNamePathOnly)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Only show the project paths. Don't show the framework versions.");

        return args;
    }
    private string _SolutionPath = string.Empty;

    protected override void OnExecute()
    {
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
            WriteLine($"Solution: {_SolutionPath}");
        }

        if (foundSolution == true)
        {
            var projects = GetResult();

            if (projects.Length == 0)
            {
                WriteLine("No projects found.");
            }
            else
            {
                WriteLine(projects);
            }
        }
    }

    internal string GetResult()
    {
        bool pathOnly = Arguments.GetBooleanValue(Constants.ArgumentNamePathOnly);

        var startInfo = new ProcessStartInfo();
        startInfo.FileName = "dotnet";

        startInfo.ArgumentList.Add("sln");
        startInfo.ArgumentList.Add(_SolutionPath);
        startInfo.ArgumentList.Add("list");
        startInfo.RedirectStandardOutput = true;

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Process start returned null");

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
                if (pathOnly == true)
                {
                    output.AppendLine(line);
                }
                else
                {
                    var solutionDir = Path.GetDirectoryName(_SolutionPath);
                    if (solutionDir == null)
                    {
                        output.AppendLine($"{line}");
                        output.AppendLine($"\tFramework: (n/a)");
                    }
                    else
                    {
                        var frameworkVersion = ProjectUtilities.GetFrameworkVersion(
                            solutionDir, line);

                        output.AppendLine($"{line}");
                        output.AppendLine($"\tFramework: {frameworkVersion}");
                    }

                }

            }

            lineNumber++;
            line = process.StandardOutput.ReadLine();
        }

        return output.ToString();
    }
}
