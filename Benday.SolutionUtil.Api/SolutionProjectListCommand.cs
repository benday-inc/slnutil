using Benday.CommandsFramework;

using System.Diagnostics;
using System.Text;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameListSolutionProjects,
        Description = "Gets list of projects in a solution.")]
public class SolutionProjectListCommand : SynchronousCommand
{

    public SolutionProjectListCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameSolutionPath).AsNotRequired()
            .WithDescription("Solution to examine. If this value is not supplied, the tool searches for a sln file automatically.");

        return args;
    }
    private string _SolutionPath;

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
