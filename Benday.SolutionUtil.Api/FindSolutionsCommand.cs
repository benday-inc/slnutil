using System.Text;
using System.Text.Json;

using Benday.CommandsFramework;
using Benday.CommandsFramework.DataFormatting;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameFindSolutions,
    Description = "Find solution files (sln and slnx) in a folder tree and optionally list projects with reference analysis."
)]
public class FindSolutionsCommand : Command
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
        
        args.AddBoolean(Constants.ArgumentNameJson)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Output results as JSON");

        args.AddBoolean(Constants.ArgumentNameSkipReferences)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Skip checking project references when listing projects in solutions");

        return args;
    }


    protected override Task OnExecute(CancellationToken cancellationToken)
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
            var formatAsJson = Arguments.GetBooleanValue(Constants.ArgumentNameJson);
            var skipReferences = Arguments.GetBooleanValue(Constants.ArgumentNameSkipReferences);

            if (listProjects == false)
            {
                if (formatAsCsv == true)
                {
                    WriteLine(FormatAsCsv(solutions));
                }
                else if (formatAsJson == true)
                {
                    WriteLine(FormatAsJson(SolutionFileJson.FromPaths(solutions)));
                }
                else
                {
                    WriteLine(FormatAsList(solutions));
                }
            }
            else
            {
                var analyses = new SolutionAnalyzer().Analyze(solutions, skipReferences);

                if (formatAsCsv == true)
                {
                    WriteLine(ListSolutionProjectsForCsv(analyses));
                }
                else if (formatAsJson == true)
                {
                    WriteLine(FormatAsJson(SolutionAnalysisJson.FromAnalyses(analyses)));
                }
                else
                {
                    WriteLine(ListSolutionProjectsForTableOutput(analyses));
                }
            }
        }

        return Task.CompletedTask;
    }

    internal string[] GetResults(string rootDir)
    {
        var solutions = Directory.GetFiles(rootDir, "*.sln", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(rootDir, "*.slnx", SearchOption.AllDirectories))
            .ToArray();
        
        var returnValues = new List<string>();
        
        // remove any zero length solutions
        foreach (var sln in solutions)
        {
            var slnFileInfo = new FileInfo(sln);

            if (slnFileInfo.Length > 0)
            {
                returnValues.Add(sln);
            }
        }

        return returnValues.ToArray();
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

    private static readonly JsonSerializerOptions _JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serializes one of the JSON view classes.  Don't pass the analysis
    /// classes to this: they hold FileInfo and DirectoryInfo values that the
    /// serializer can't walk.  See <see cref="SolutionAnalysisJson"/>.
    /// </summary>
    internal string FormatAsJson<T>(List<T> items)
    {
        return JsonSerializer.Serialize(items, _JsonOptions);
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

    private const string NotApplicable = "n/a";

    private static readonly string[] _ProjectInfoColumnNamesForCsv = new[]
    {
        "solution-filename",
        "project",
        "reference-type",
        "reference-target",
        "outside-of-solution-root",
        "reference-target-path",
        "solution-path-depth",
        "project-path-depth",
        "solution-dir",
        "project-dir",
        "uses-packages-config",
        "target-framework"
    };
    
    private static readonly string[] _ProjectInfoColumnNamesForTable = new[]
    {
        "solution-filename",
        "project",
        "reference-type",
        "reference-target",
        "target-framework"
    };

    internal string ListSolutionProjectsForCsv(
        List<SolutionAnalysis> analyses)
    {
        var returnValue = new CsvWriter();

        foreach (var columnName in _ProjectInfoColumnNamesForCsv)
        {
            returnValue.AddColumn(columnName);
        }

        foreach (var row in GetProjectInfoRows(analyses, OutputFormat.Csv))
        {
            returnValue.AddRow(row);
        }

        return returnValue.ToCsvString();
    }

    internal string ListSolutionProjectsForTableOutput(List<SolutionAnalysis> analyses)
    {
        var returnValue = new TableFormatter();

        foreach (var columnName in _ProjectInfoColumnNamesForTable)
        {
            returnValue.AddColumn(columnName);
        }

        foreach (var row in GetProjectInfoRows(analyses, OutputFormat.Table))
        {
            returnValue.AddData(row);
        }

        return returnValue.FormatTable();
    }

    private List<string[]> GetProjectInfoRows(List<SolutionAnalysis> analyses, OutputFormat outputFormat)
    {
        var returnValues = new List<string[]>();

        foreach (var solution in analyses)
        {
            foreach (var project in solution.Projects)
            {
                if (project.References.Count == 0)
                {
                    returnValues.Add(
                        GetRowForProjectWithoutReferences(solution, project, outputFormat));
                }
                else
                {
                    foreach (var reference in project.References)
                    {
                        returnValues.Add(GetRowForReference(solution, project, reference, outputFormat));
                    }
                }
            }
        }

        return returnValues;
    }

    private string[] GetRowForProjectWithoutReferences(
        SolutionAnalysis solution, ProjectAnalysis project, OutputFormat outputFormat)
    {
        var referenceType = project.Exists == false ? "project-not-found" : NotApplicable;

        if (outputFormat == OutputFormat.Table)
        {
            return new[]
            {
                solution.SolutionFileName,
                project.ProjectFileName,

                // reference stuff
                referenceType,
                NotApplicable,

                // per-project metadata
                project.TargetFramework
            };
        }
        else if (outputFormat != OutputFormat.Csv)
        {
            throw new ArgumentException("Invalid output format.", nameof(outputFormat));
        }

        return new[]
        {
            solution.SolutionFileName,
            project.ProjectFileName,

            // reference stuff
            referenceType,
            NotApplicable,
            NotApplicable,
            NotApplicable,

            // file structure stuff
            solution.SolutionPathDepth.ToString(),
            project.ProjectPathDepth.ToString(),

            // solution stuff
            solution.SolutionDirectory.FullName,
            project.ProjectDirectory.FullName,

            // per-project metadata
            project.UsesPackagesConfig.ToString(),
            project.TargetFramework
        };
    }

    private enum OutputFormat
    {
        Csv,
        Table,
        Json
    }
    
    private string[] GetRowForReference(
        SolutionAnalysis solution, ProjectAnalysis project, 
        ReferenceAnalysis reference, OutputFormat outputFormat)
    {
        if (outputFormat == OutputFormat.Csv)
        {
            return new[]
            {
                solution.SolutionFileName, project.ProjectFileName,

                // reference stuff
                reference.ReferenceType, reference.ReferenceTargetName, reference.IsOutsideOfSolutionRoot.ToString(),
                reference.ReferenceTarget,

                // file structure stuff
                solution.SolutionPathDepth.ToString(), project.ProjectPathDepth.ToString(),

                // solution stuff
                solution.SolutionDirectory.FullName, project.ProjectDirectory.FullName,

                // per-project metadata
                project.UsesPackagesConfig.ToString(), project.TargetFramework
            };
        }
        else if (outputFormat == OutputFormat.Table)
        {
            return new[]
            {
                solution.SolutionFileName, project.ProjectFileName,

                // reference stuff
                reference.ReferenceType, reference.ReferenceTargetName, project.TargetFramework
            };
        }
        else
        {
            throw new ArgumentException("Invalid output format.");
        }
    }
}
