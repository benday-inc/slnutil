namespace Benday.SolutionUtil.Api;

/// <summary>
/// JSON-friendly view of a <see cref="SolutionAnalysis"/>.
///
/// The analysis classes hold FileInfo and DirectoryInfo values and those can't
/// be handed to JsonSerializer directly.  DirectoryInfo exposes Parent and Root
/// as more DirectoryInfo instances and a root directory is its own Root, so the
/// serializer walks that chain until it trips the max depth limit and reports
/// an object cycle.  Every property it touches on the way down is a filesystem
/// call, which is why it takes so long to get to the failure.
/// </summary>
public class SolutionAnalysisJson
{
    public string SolutionFileName { get; set; } = string.Empty;

    public string SolutionPath { get; set; } = string.Empty;

    public string SolutionDirectory { get; set; } = string.Empty;

    public int SolutionPathDepth { get; set; }

    public List<ProjectAnalysisJson> Projects { get; set; } = new();

    public static SolutionAnalysisJson FromAnalysis(SolutionAnalysis analysis)
    {
        var returnValue = new SolutionAnalysisJson()
        {
            SolutionFileName = analysis.SolutionFileName,
            SolutionPath = analysis.SolutionPath.FullName,
            SolutionDirectory = analysis.SolutionDirectory.FullName,
            SolutionPathDepth = analysis.SolutionPathDepth
        };

        foreach (var project in analysis.Projects)
        {
            returnValue.Projects.Add(ProjectAnalysisJson.FromAnalysis(project));
        }

        return returnValue;
    }

    public static List<SolutionAnalysisJson> FromAnalyses(IEnumerable<SolutionAnalysis> analyses)
    {
        var returnValues = new List<SolutionAnalysisJson>();

        foreach (var analysis in analyses)
        {
            returnValues.Add(FromAnalysis(analysis));
        }

        return returnValues;
    }
}

/// <summary>
/// JSON-friendly view of a <see cref="ProjectAnalysis"/>.
/// </summary>
public class ProjectAnalysisJson
{
    public string ProjectFileName { get; set; } = string.Empty;

    public string ProjectPath { get; set; } = string.Empty;

    public string ProjectDirectory { get; set; } = string.Empty;

    public bool Exists { get; set; }

    public int ProjectPathDepth { get; set; }

    public bool UsesPackagesConfig { get; set; }

    public string TargetFramework { get; set; } = string.Empty;

    public List<ReferenceAnalysisJson> References { get; set; } = new();

    public static ProjectAnalysisJson FromAnalysis(ProjectAnalysis analysis)
    {
        var returnValue = new ProjectAnalysisJson()
        {
            ProjectFileName = analysis.ProjectFileName,
            ProjectPath = analysis.ProjectPath.FullName,
            ProjectDirectory = analysis.ProjectDirectory.FullName,
            Exists = analysis.Exists,
            ProjectPathDepth = analysis.ProjectPathDepth,
            UsesPackagesConfig = analysis.UsesPackagesConfig,
            TargetFramework = analysis.TargetFramework
        };

        foreach (var reference in analysis.References)
        {
            returnValue.References.Add(ReferenceAnalysisJson.FromAnalysis(reference));
        }

        return returnValue;
    }
}

/// <summary>
/// JSON-friendly view of a <see cref="ReferenceAnalysis"/>.
/// </summary>
public class ReferenceAnalysisJson
{
    public string ReferenceType { get; set; } = string.Empty;

    public string ReferenceTarget { get; set; } = string.Empty;

    public string ReferenceTargetName { get; set; } = string.Empty;

    public bool IsOutsideOfSolutionRoot { get; set; }

    public static ReferenceAnalysisJson FromAnalysis(ReferenceAnalysis analysis)
    {
        return new ReferenceAnalysisJson()
        {
            ReferenceType = analysis.ReferenceType,
            ReferenceTarget = analysis.ReferenceTarget,
            ReferenceTargetName = analysis.ReferenceTargetName,
            IsOutsideOfSolutionRoot = analysis.IsOutsideOfSolutionRoot
        };
    }
}

/// <summary>
/// JSON-friendly view of a solution file for the case where the caller asked
/// for JSON without asking for the projects inside each solution.  Mirrors the
/// columns that the CSV output uses for the same case.
/// </summary>
public class SolutionFileJson
{
    public string SolutionFileName { get; set; } = string.Empty;

    public string SolutionDirectory { get; set; } = string.Empty;

    public string SolutionDirectoryFullPath { get; set; } = string.Empty;

    public string SolutionPath { get; set; } = string.Empty;

    public static List<SolutionFileJson> FromPaths(IEnumerable<string> solutionPaths)
    {
        var returnValues = new List<SolutionFileJson>();

        foreach (var item in solutionPaths)
        {
            var info = new FileInfo(item);

            returnValues.Add(new SolutionFileJson()
            {
                SolutionFileName = info.Name,
                SolutionDirectory = info.Directory!.Name,
                SolutionDirectoryFullPath = info.Directory!.FullName,
                SolutionPath = item
            });
        }

        return returnValues;
    }
}
