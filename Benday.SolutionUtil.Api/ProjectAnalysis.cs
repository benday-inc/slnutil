namespace Benday.SolutionUtil.Api;

/// <summary>
/// A project that's listed in a solution, plus the references it declares.
/// </summary>
public class ProjectAnalysis
{
    public ProjectAnalysis(FileInfo projectPath)
    {
        ProjectPath = projectPath;

        ProjectDirectory = projectPath.Directory ??
            throw new InvalidOperationException(
                $"Project file '{projectPath.FullName}' has a null directory.");
    }

    public FileInfo ProjectPath { get; }

    public DirectoryInfo ProjectDirectory { get; }

    public string ProjectFileName => ProjectPath.Name;

    /// <summary>
    /// False when the solution lists a project file that isn't on disk.  When
    /// this is false, the rest of the project metadata is left at defaults
    /// because there's no file to read it from.
    /// </summary>
    public bool Exists { get; set; }

    public int ProjectPathDepth { get; set; }

    public bool UsesPackagesConfig { get; set; }

    public string TargetFramework { get; set; } = string.Empty;

    public List<ReferenceAnalysis> References { get; set; } = new();
}
