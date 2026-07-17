namespace Benday.SolutionUtil.Api;

/// <summary>
/// A reference from a project to another project, a binary, or a NuGet package.
/// </summary>
public class ReferenceAnalysis
{
    public string ReferenceType { get; set; } = string.Empty;

    /// <summary>
    /// The reference target as it appears in the project file.
    /// </summary>
    public string ReferenceTarget { get; set; } = string.Empty;

    /// <summary>
    /// The file name portion of the reference target.
    /// </summary>
    public string ReferenceTargetName => Path.GetFileName(ReferenceTarget);

    /// <summary>
    /// True if the reference target resolves to a location outside of the
    /// directory tree that contains the solution.  Only meaningful for
    /// reference types that resolve to a path.
    /// </summary>
    public bool IsOutsideOfSolutionRoot { get; set; }
}
