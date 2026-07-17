namespace Benday.SolutionUtil.Api;

/// <summary>
/// A solution file and the projects it contains.
/// </summary>
public class SolutionAnalysis
{
    public SolutionAnalysis(FileInfo solutionPath)
    {
        SolutionPath = solutionPath;

        SolutionDirectory = solutionPath.Directory ??
            throw new InvalidOperationException(
                $"Solution file '{solutionPath.FullName}' has a null directory.");
    }

    public FileInfo SolutionPath { get; }

    public DirectoryInfo SolutionDirectory { get; }

    public string SolutionFileName => SolutionPath.Name;

    public int SolutionPathDepth { get; set; }

    public List<ProjectAnalysis> Projects { get; set; } = new();
}
