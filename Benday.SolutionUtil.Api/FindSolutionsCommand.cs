using System.Diagnostics;
using System.Text;

using Benday.CommandsFramework;
using Benday.CommandsFramework.DataFormatting;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameFindSolutions,
    Description = "Find solution files (sln and slnx) in a folder tree and optionally list projects with reference analysis."
)]
public class FindSolutionsCommand : SynchronousCommand
{

    public FindSolutionsCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameRootDirectory)
            .AsNotRequired()
            .WithDescription("Path to start search from.  Defaults to current directory.")
            .WithDefaultValue(Directory.GetCurrentDirectory());


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
            .WithDescription("Skip checking project references when listing projects in solutions");

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
        var solutions = Directory.GetFiles(rootDir, "*.sln", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(rootDir, "*.slnx", SearchOption.AllDirectories))
            .ToArray();

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
 

        var returnValue = new CsvWriter();

        // write header
        returnValue.AddColumn("solution-filename");
        
        returnValue.AddColumn("project");
        
        returnValue.AddColumn("reference-type");
        
        returnValue.AddColumn("reference-target");
        
        returnValue.AddColumn("outside-of-solution-root");
        
        returnValue.AddColumn("reference-target-path");
        
        returnValue.AddColumn("solution-path-depth");
        
        returnValue.AddColumn("project-path-depth");
        
        returnValue.AddColumn("solution-dir");

        returnValue.AddColumn("project-dir");

        returnValue.AddColumn("uses-packages-config");

        returnValue.AddColumn("target-framework");

        foreach (var item in solutions)
        {
            AppendProjectInfo(returnValue, item, skipReferences);
        }

        return returnValue.ToCsvString();
    }

    private int GetPathDepth(string dirPath)
    {
        string[] directories = dirPath.Split(Path.DirectorySeparatorChar);

        return directories.Length;
    }

    private void AppendProjectInfo(CsvWriter returnValue, string solutionPath, bool skipReferences)
    {
        FileInfo solutionFileInfo = new FileInfo(solutionPath);
        var solutionDir = solutionFileInfo.Directory ??
            throw new InvalidOperationException($"Solution file has a null directory.");

        var solutionPathDepth = GetPathDepth(solutionFileInfo.Directory.FullName);

        FileInfo projectFileInfo;

        var solutionProjects = GetProjects(solutionPath);

        foreach (var item in solutionProjects)
        {
            projectFileInfo = new FileInfo(Path.Combine(solutionDir.FullName, item));

            var usesPackagesConfig = projectFileInfo.Exists
                ? ProjectUtilities.ProjectUsesPackagesConfig(projectFileInfo.FullName)
                : false;

            var targetFramework = projectFileInfo.Exists
                ? ProjectUtilities.GetProjectTargetFrameworkShortForm(projectFileInfo.FullName)
                : string.Empty;

            if (projectFileInfo.Exists == false)
            {
                AppendInfoForProjectNotFound(returnValue, solutionFileInfo, solutionDir, solutionPathDepth, projectFileInfo, usesPackagesConfig, targetFramework);
            }
            else if (skipReferences == true)
            {
                AppendInfoForProjectWithoutReferences(returnValue, solutionFileInfo, solutionDir, solutionPathDepth, projectFileInfo, usesPackagesConfig, targetFramework);
            }
            else
            {
                var references = ProjectUtilities.GetReferenceForProjectFile(projectFileInfo.FullName);

                if (references == null || references.Count == 0)
                {
                    AppendInfoForProjectWithoutReferences(returnValue, solutionFileInfo, solutionDir, solutionPathDepth, projectFileInfo, usesPackagesConfig, targetFramework);
                }
                else
                {
                    AppendInfoForProjectWithReferences(returnValue, solutionFileInfo, solutionDir, solutionPathDepth, projectFileInfo, references, usesPackagesConfig, targetFramework);
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

    private void AppendInfoForProjectWithReferences(CsvWriter returnValue,
        FileInfo solutionFileInfo,
        DirectoryInfo solutionDir,
        int solutionPathDepth,
        FileInfo projectFileInfo,
        List<ReferenceInfo> references,
        bool usesPackagesConfig,
        string targetFramework)
    {
        foreach (var reference in references)
        {
            var rowValues = new List<string>();

            rowValues.Add(solutionFileInfo.Name);

            rowValues.Add(projectFileInfo.Name);


            // reference stuff
            rowValues.Add(reference.ReferenceType);
            rowValues.Add(Path.GetFileName(reference.ReferenceTarget));

            if (projectFileInfo.Directory == null)
            {
                throw new InvalidOperationException($"projectFileInfo.Directory is null.");
            }

            if (reference.ReferenceType == "project-ref" ||
                reference.ReferenceType == "binary-ref" ||
                reference.ReferenceType == "nuget-via-packages-config")
            {
                rowValues.Add(IsReferenceOutsideOfSolutionRoot(
                    reference.ReferenceTarget,
                    solutionDir,
                    projectFileInfo.Directory).ToString());
            }
            else
            {
                rowValues.Add(false.ToString());
            }

            rowValues.Add(reference.ReferenceTarget);

            // file structure stuff
            rowValues.Add(solutionPathDepth.ToString());
            rowValues.Add(GetPathDepth(projectFileInfo.Directory.FullName).ToString());


            // solution stuff
            rowValues.Add(solutionDir.FullName);

            rowValues.Add(projectFileInfo.Directory.FullName);

            // per-project metadata
            rowValues.Add(usesPackagesConfig.ToString());

            rowValues.Add(targetFramework);

            returnValue.AddRow(rowValues.ToArray());
        }
    }

    private void AppendInfoForProjectNotFound(CsvWriter returnValue, FileInfo solutionFileInfo, DirectoryInfo solutionDir, int solutionPathDepth, FileInfo projectFileInfo, bool usesPackagesConfig, string targetFramework)
    {
        var rowValues = new List<string>();

        rowValues.Add(solutionFileInfo.Name);

        rowValues.Add(projectFileInfo.Name);

        // reference stuff
        rowValues.Add("project-not-found");

        rowValues.Add("n/a");

        rowValues.Add("n/a");

        rowValues.Add("n/a");

        // file structure stuff
        rowValues.Add(solutionPathDepth.ToString());

        rowValues.Add(GetPathDepth(projectFileInfo.Directory!.FullName).ToString());

        // solution stuff
        rowValues.Add(solutionDir.FullName);

        rowValues.Add(projectFileInfo.Directory.FullName);

        // per-project metadata
        rowValues.Add(usesPackagesConfig.ToString());

        rowValues.Add(targetFramework);

        returnValue.AddRow(rowValues.ToArray());

    }

    private void AppendInfoForProjectWithoutReferences(CsvWriter returnValue, FileInfo solutionFileInfo, DirectoryInfo solutionDir, int solutionPathDepth, FileInfo projectFileInfo, bool usesPackagesConfig, string targetFramework)
    {
        var rowValues = new List<string>();

        rowValues.Add(solutionFileInfo.Name);

        rowValues.Add(projectFileInfo.Name);

        // reference stuff
        rowValues.Add("n/a");

        rowValues.Add("n/a");

        rowValues.Add("n/a");

        rowValues.Add("n/a");

        // file structure stuff
        rowValues.Add(solutionPathDepth.ToString());

        rowValues.Add(GetPathDepth(projectFileInfo.Directory!.FullName).ToString());

        // solution stuff
        rowValues.Add(solutionDir.FullName);

        rowValues.Add(projectFileInfo.Directory.FullName);

        // per-project metadata
        rowValues.Add(usesPackagesConfig.ToString());

        rowValues.Add(targetFramework);

        returnValue.AddRow(rowValues.ToArray());

    }

    private List<string> GetProjects(string solutionPath)
    {
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = "dotnet";

        startInfo.ArgumentList.Add("sln");
        startInfo.ArgumentList.Add(solutionPath);
        startInfo.ArgumentList.Add("list");
        startInfo.RedirectStandardOutput = true;

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Process.Start() returned a null."); ;

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

