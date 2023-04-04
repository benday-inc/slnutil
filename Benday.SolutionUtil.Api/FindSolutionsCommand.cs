using System.Diagnostics;
using System.Text;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;
[Command(Name = Constants.CommandArgumentNameFindSolutions, Description = "Find solution files in a folder tree.")]
public class FindSolutionsCommand : SynchronousCommand
{

    public FindSolutionsCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameRootDirectory).
            WithDescription("Path to start search from");

        args.AddBoolean(Constants.ArgumentNameListProjects)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("List projects in solutions");

        args.AddBoolean(Constants.ArgumentNameCommaSeparatedValues)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Output results as comma-separated values");

        args.AddBoolean(Constants.ArgumentNameSkipReferences)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Output results as comma-separated values");

        return args;
    }


    protected override void OnExecute()
    {
        var rootDirPath = Arguments.GetStringValue(Constants.ArgumentNameRootDirectory);

        if (Directory.Exists(rootDirPath) == false)
        {
            throw new KnownException($"Root directory for search does not exist. '{rootDirPath}'");
        }

        string[] solutions = GetResults(rootDirPath);

        if (solutions.Length == 0)
        {
            WriteLine("No solutions found.");
        }
        else
        {
            var listProjects = Arguments.GetBooleanValue(Constants.ArgumentNameListProjects);
            var formatAsCsv = Arguments.GetBooleanValue(Constants.ArgumentNameCommaSeparatedValues);
            var skipReferences = Arguments.GetBooleanValue(Constants.ArgumentNameSkipReferences);

            if (listProjects == false)
            {
                if (formatAsCsv == true)
                {
                    WriteLine(FormatAsCsv(solutions));
                }
                else
                {
                    WriteLine(FormatAsList(solutions));
                }
            }
            else
            {
                WriteLine(ListSolutionProjects(solutions, skipReferences));
            }
        }
    }

    internal string[] GetResults(string rootDir)
    {
        var solutions = Directory.GetFiles(
            rootDir, "*.sln", SearchOption.AllDirectories);

        return solutions;
    }

    private string FormatAsCsv(IEnumerable<string> items)
    {
        var returnValue = new StringBuilder();

        // write header
        returnValue.Append("solution-filename");
        returnValue.Append(",");

        returnValue.Append("solution-directory");
        returnValue.Append(",");

        returnValue.Append("solution-directory-fullpath");
        returnValue.Append(",");

        returnValue.Append("solution-fullpath");
        returnValue.AppendLine();

        FileInfo info;

        foreach (var item in items)
        {
            info = new FileInfo(item);

            returnValue.Append(info.Name);
            returnValue.Append(",");

            returnValue.Append(info.Directory!.Name);
            returnValue.Append(",");

            returnValue.Append(info.Directory!.FullName);
            returnValue.Append(",");

            returnValue.AppendLine(item);
        }

        return returnValue.ToString();
    }

    private string FormatAsList(IEnumerable<string> items)
    {
        var returnValue = new StringBuilder();

        foreach (var item in items)
        {
            returnValue.AppendLine(item);
        }

        return returnValue.ToString();
    }

    private string ListSolutionProjects(string[] solutions, bool skipReferences)
    {
        var returnValue = new StringBuilder();

        // write header
        returnValue.Append("solution-filename");
        returnValue.Append(",");

        returnValue.Append("project");
        returnValue.Append(",");

        returnValue.Append("reference-type");
        returnValue.Append(",");

        returnValue.Append("reference-target");
        returnValue.Append(",");

        returnValue.Append("outside-of-solution-root");
        returnValue.Append(",");

        returnValue.Append("reference-target-path");
        returnValue.Append(",");

        returnValue.Append("solution-path-depth");
        returnValue.Append(",");

        returnValue.Append("project-path-depth");
        returnValue.Append(",");

        returnValue.Append("solution-dir");
        returnValue.Append(",");

        returnValue.Append("project-dir");
        returnValue.AppendLine();

        foreach (var item in solutions)
        {
            AppendProjectInfo(returnValue, item, skipReferences);
        }

        return returnValue.ToString();
    }

    private int GetPathDepth(string dirPath)
    {
        string[] directories = dirPath.Split(Path.DirectorySeparatorChar);

        return directories.Length;
    }

    private void AppendProjectInfo(StringBuilder returnValue, string solutionPath, bool skipReferences)
    {
        FileInfo solutionFileInfo = new FileInfo(solutionPath);
        var solutionDir = solutionFileInfo.Directory;

        var solutionPathDepth = GetPathDepth(solutionFileInfo.Directory.FullName);

        FileInfo projectFileInfo;

        var solutionProjects = GetProjects(solutionPath);

        foreach (var item in solutionProjects)
        {
            projectFileInfo = new FileInfo(Path.Combine(solutionDir.FullName, item));

            if (projectFileInfo.Exists == false)
            {
                AppendInfoForProjectNotFound(returnValue, solutionFileInfo, solutionDir, solutionPathDepth, projectFileInfo);
            }
            else if (skipReferences == true)
            {
                AppendInfoForProjectWithoutReferences(returnValue, solutionFileInfo, solutionDir, solutionPathDepth, projectFileInfo);
            }
            else
            {
                var references = ProjectUtilities.GetReferenceForProjectFile(projectFileInfo.FullName);

                if (references == null || references.Count == 0)
                {
                    AppendInfoForProjectWithoutReferences(returnValue, solutionFileInfo, solutionDir, solutionPathDepth, projectFileInfo);
                }
                else
                {
                    AppendInfoForProjectWithReferences(returnValue, solutionFileInfo, solutionDir, solutionPathDepth, projectFileInfo, references);
                }
            }
        }
    }

    private bool IsReferenceOutsideOfSolutionRoot(string referenceTarget, DirectoryInfo solutionDir, DirectoryInfo directory)
    {
        var referencePath = Path.Combine(directory.FullName, referenceTarget);

        var referenceDir = new DirectoryInfo(referencePath);

        if (referenceDir.FullName.ToLower().StartsWith(solutionDir.FullName.ToLower()) == true)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private void AppendInfoForProjectWithReferences(StringBuilder returnValue,
        FileInfo solutionFileInfo,
        DirectoryInfo solutionDir,
        int solutionPathDepth,
        FileInfo projectFileInfo,
        List<ReferenceInfo> references)
    {
        foreach (var reference in references)
        {
            returnValue.Append(solutionFileInfo.Name);
            returnValue.Append(",");

            returnValue.Append(projectFileInfo.Name);
            returnValue.Append(",");

            // reference stuff
            returnValue.Append(reference.ReferenceType);
            returnValue.Append(",");

            returnValue.Append(Path.GetFileName(reference.ReferenceTarget));
            returnValue.Append(",");

            if (reference.ReferenceType == "project-ref")
            {
                returnValue.Append(IsReferenceOutsideOfSolutionRoot(
                    reference.ReferenceTarget,
                    solutionDir,
                    projectFileInfo.Directory));
                returnValue.Append(",");
            }
            else if (reference.ReferenceType == "binary-ref")
            {
                returnValue.Append(IsReferenceOutsideOfSolutionRoot(
                    reference.ReferenceTarget,
                    solutionDir,
                    projectFileInfo.Directory));
                returnValue.Append(",");
            }
            else
            {
                returnValue.Append(false);
                returnValue.Append(",");
            }

            returnValue.Append(reference.ReferenceTarget);
            returnValue.Append(",");

            // file structure stuff
            returnValue.Append(solutionPathDepth);
            returnValue.Append(",");

            returnValue.Append(GetPathDepth(projectFileInfo.Directory.FullName));
            returnValue.Append(",");

            // solution stuff
            returnValue.Append(solutionDir.FullName);
            returnValue.Append(",");

            returnValue.Append(projectFileInfo.Directory.FullName);
            returnValue.AppendLine();
        }
    }

    private void AppendInfoForProjectNotFound(StringBuilder returnValue, FileInfo solutionFileInfo, DirectoryInfo solutionDir, int solutionPathDepth, FileInfo projectFileInfo)
    {
        returnValue.Append(solutionFileInfo.Name);
        returnValue.Append(",");

        returnValue.Append(projectFileInfo.Name);
        returnValue.Append(",");

        // reference stuff
        returnValue.Append("project-not-found");
        returnValue.Append(",");

        returnValue.Append("n/a");
        returnValue.Append(",");

        returnValue.Append("n/a");
        returnValue.Append(",");

        returnValue.Append("n/a");
        returnValue.Append(",");

        // file structure stuff
        returnValue.Append(solutionPathDepth);
        returnValue.Append(",");

        returnValue.Append(GetPathDepth(projectFileInfo.Directory.FullName));
        returnValue.Append(",");

        // solution stuff
        returnValue.Append(solutionDir.FullName);
        returnValue.Append(",");

        returnValue.Append(projectFileInfo.Directory.FullName);
        returnValue.AppendLine();
    }

    private void AppendInfoForProjectWithoutReferences(StringBuilder returnValue, FileInfo solutionFileInfo, DirectoryInfo solutionDir, int solutionPathDepth, FileInfo projectFileInfo)
    {
        returnValue.Append(solutionFileInfo.Name);
        returnValue.Append(",");

        returnValue.Append(projectFileInfo.Name);
        returnValue.Append(",");

        // reference stuff
        returnValue.Append("n/a");
        returnValue.Append(",");

        returnValue.Append("n/a");
        returnValue.Append(",");

        returnValue.Append("n/a");
        returnValue.Append(",");

        returnValue.Append("n/a");
        returnValue.Append(",");

        // file structure stuff
        returnValue.Append(solutionPathDepth);
        returnValue.Append(",");

        returnValue.Append(GetPathDepth(projectFileInfo.Directory.FullName));
        returnValue.Append(",");

        // solution stuff
        returnValue.Append(solutionDir.FullName);
        returnValue.Append(",");

        returnValue.Append(projectFileInfo.Directory.FullName);
        returnValue.AppendLine();
    }

    private List<string> GetProjects(string solutionPath)
    {
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = "dotnet";

        startInfo.ArgumentList.Add("sln");
        startInfo.ArgumentList.Add(solutionPath);
        startInfo.ArgumentList.Add("list");
        startInfo.RedirectStandardOutput = true;

        var process = Process.Start(startInfo);

        process.WaitForExit();

        var returnValues = new List<string>();

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
                returnValues.Add(line);
            }

            lineNumber++;
            line = process.StandardOutput.ReadLine();
        }

        return returnValues;
    }
}
