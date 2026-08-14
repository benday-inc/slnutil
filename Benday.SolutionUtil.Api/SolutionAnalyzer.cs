using System.Diagnostics;

namespace Benday.SolutionUtil.Api;

/// <summary>
/// Discovers the projects in a solution and the references those projects
/// declare.  Returns the results as data so that callers can decide how to
/// present them.
/// </summary>
public class SolutionAnalyzer
{
    public List<SolutionAnalysis> Analyze(IEnumerable<string> solutionPaths, bool skipReferences)
    {
        var returnValues = new List<SolutionAnalysis>();

        foreach (var item in solutionPaths)
        {
            returnValues.Add(Analyze(item, skipReferences));
        }

        return returnValues;
    }

    public SolutionAnalysis Analyze(string solutionPath, bool skipReferences)
    {
        var returnValue = new SolutionAnalysis(new FileInfo(solutionPath));

        returnValue.SolutionPathDepth = GetPathDepth(returnValue.SolutionDirectory.FullName);

        foreach (var item in GetProjects(solutionPath))
        {
            returnValue.Projects.Add(AnalyzeProject(returnValue, item, skipReferences));
        }

        return returnValue;
    }

    private ProjectAnalysis AnalyzeProject(
        SolutionAnalysis solution, string projectPathFromSolution, bool skipReferences)
    {
        var projectFileInfo = new FileInfo(
            Path.Combine(solution.SolutionDirectory.FullName, projectPathFromSolution));

        var returnValue = new ProjectAnalysis(projectFileInfo);

        returnValue.Exists = projectFileInfo.Exists;
        returnValue.ProjectPathDepth = GetPathDepth(returnValue.ProjectDirectory.FullName);

        if (returnValue.Exists == false)
        {
            return returnValue;
        }

        returnValue.UsesPackagesConfig =
            ProjectUtilities.ProjectUsesPackagesConfig(projectFileInfo.FullName);

        returnValue.TargetFramework =
            ProjectUtilities.GetProjectTargetFrameworkShortForm(projectFileInfo.FullName);

        if (skipReferences == true)
        {
            return returnValue;
        }

        var references = ProjectUtilities.GetReferenceForProjectFile(projectFileInfo.FullName);

        if (references != null)
        {
            foreach (var reference in references)
            {
                returnValue.References.Add(AnalyzeReference(solution, returnValue, reference));
            }
        }

        return returnValue;
    }

    private ReferenceAnalysis AnalyzeReference(
        SolutionAnalysis solution, ProjectAnalysis project, ReferenceInfo reference)
    {
        var returnValue = new ReferenceAnalysis()
        {
            ReferenceType = reference.ReferenceType,
            ReferenceTarget = reference.ReferenceTarget
        };

        if (reference.ReferenceType == "project-ref" ||
            reference.ReferenceType == "binary-ref" ||
            reference.ReferenceType == "nuget-via-packages-config")
        {
            returnValue.IsOutsideOfSolutionRoot = IsReferenceOutsideOfSolutionRoot(
                reference.ReferenceTarget,
                solution.SolutionDirectory,
                project.ProjectDirectory);
        }

        return returnValue;
    }

    private bool IsReferenceOutsideOfSolutionRoot(
        string referenceTarget, DirectoryInfo solutionDir, DirectoryInfo directory)
    {
        var referencePath = ResolveReferencePath(directory.FullName, referenceTarget);

        return IsInsideDirectory(referencePath, solutionDir.FullName) == false;
    }

    /// <summary>
    /// Resolves a reference target against the folder holding the project.
    ///
    /// Project files are written on Windows and use backslashes, but this tool
    /// also runs on macOS and Linux where a backslash is an ordinary filename
    /// character.  Without converting first, Path.GetFullPath leaves the ".."
    /// segments in place and every comparison made afterwards is wrong.
    /// </summary>
    public static string ResolveReferencePath(string baseDirectory, string referenceTarget)
    {
        var target = (referenceTarget ?? string.Empty)
            .Trim()
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.GetFullPath(Path.Combine(baseDirectory, target));
    }

    /// <summary>
    /// True when the path sits inside the directory.  The comparison is made on
    /// a directory boundary rather than on the raw string, so a sibling that
    /// merely starts the same way -- "/src/App.Tests" next to "/src/App" --
    /// does not read as being inside it.
    /// </summary>
    public static bool IsInsideDirectory(string path, string directory)
    {
        var comparison = GetPathComparison();

        var root = directory.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(path, root, comparison) == true)
        {
            return true;
        }

        return path.StartsWith(root + Path.DirectorySeparatorChar, comparison);
    }

    /// <summary>
    /// Windows and macOS treat paths as case-insensitive; Linux does not.
    /// </summary>
    private static StringComparison GetPathComparison()
    {
        if (OperatingSystem.IsLinux() == true)
        {
            return StringComparison.Ordinal;
        }

        return StringComparison.OrdinalIgnoreCase;
    }

    private int GetPathDepth(string dirPath)
    {
        string[] directories = dirPath.Split(Path.DirectorySeparatorChar);

        return directories.Length;
    }

    private List<string> GetProjects(string solutionPath)
    {
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = "dotnet";

        startInfo.ArgumentList.Add("sln");
        startInfo.ArgumentList.Add(solutionPath);
        startInfo.ArgumentList.Add("list");
        startInfo.RedirectStandardOutput = true;

        var process = Process.Start(startInfo) ??
            throw new InvalidOperationException($"Process.Start() returned a null.");

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
