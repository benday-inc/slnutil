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
