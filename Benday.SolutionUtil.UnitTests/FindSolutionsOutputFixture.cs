using System.Text.Json;

using Benday.CommandsFramework;
using Benday.Common.Testing;
using Benday.SolutionUtil.Api;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

/// <summary>
/// Covers the output formatting for findsolutions --listprojects.
/// </summary>
public class FindSolutionsOutputFixture : TestClassBase
{
    public FindSolutionsOutputFixture(ITestOutputHelper output) : base(output)
    {
    }

    private FindSolutionsCommand? _SystemUnderTest;

    private FindSolutionsCommand SystemUnderTest
    {
        get
        {
            _SystemUnderTest ??= new FindSolutionsCommand(
                new CommandExecutionInfo(), new StringBuilderTextOutputProvider());

            return _SystemUnderTest;
        }
    }

    private static string PathFor(params string[] segments)
    {
        var root = OperatingSystem.IsWindows() == true ? @"C:\" : "/";

        return Path.Combine(root, Path.Combine(segments));
    }

    /// <summary>
    /// A solution holding one project.  The project has no references unless
    /// the caller adds some, which is the case that used to break the table.
    /// </summary>
    private static SolutionAnalysis GetAnalysis()
    {
        var solution = new SolutionAnalysis(
            new FileInfo(PathFor("src", "App", "App.sln")))
        {
            SolutionPathDepth = 3
        };

        var project = new ProjectAnalysis(
            new FileInfo(PathFor("src", "App", "Web", "Web.csproj")))
        {
            Exists = true,
            ProjectPathDepth = 4,
            UsesPackagesConfig = false,
            TargetFramework = "net10.0"
        };

        solution.Projects.Add(project);

        return solution;
    }

    [Fact]
    public void TableOutputHandlesProjectWithoutReferences()
    {
        // The row for a project with no references used to be built with the
        // twelve CSV columns no matter which format was being written, so the
        // table formatter rejected it: "Expected 5 columns but received 12".
        var analyses = new List<SolutionAnalysis>() { GetAnalysis() };

        var actual = SystemUnderTest.ListSolutionProjectsForTableOutput(analyses);

        WriteLine(actual);

        Assert.Contains("App.sln", actual);
        Assert.Contains("Web.csproj", actual);
        Assert.Contains("net10.0", actual);
    }

    [Fact]
    public void TableOutputHandlesProjectWithReferences()
    {
        var analysis = GetAnalysis();

        analysis.Projects[0].References.Add(new ReferenceAnalysis()
        {
            ReferenceType = "project-ref",
            ReferenceTarget = @"..\Core\Core.csproj"
        });

        var actual = SystemUnderTest.ListSolutionProjectsForTableOutput(
            new List<SolutionAnalysis>() { analysis });

        WriteLine(actual);

        Assert.Contains("project-ref", actual);
        Assert.Contains("Core.csproj", actual);
    }

    [Fact]
    public void CsvOutputHandlesProjectWithoutReferences()
    {
        var analyses = new List<SolutionAnalysis>() { GetAnalysis() };

        var actual = SystemUnderTest.ListSolutionProjectsForCsv(analyses);

        WriteLine(actual);

        Assert.Contains("App.sln", actual);
        Assert.Contains("Web.csproj", actual);
        Assert.Contains("uses-packages-config", actual);
    }

    [Fact]
    public void MissingProjectIsReportedInTheTable()
    {
        var analysis = GetAnalysis();

        analysis.Projects[0].Exists = false;

        var actual = SystemUnderTest.ListSolutionProjectsForTableOutput(
            new List<SolutionAnalysis>() { analysis });

        WriteLine(actual);

        Assert.Contains("project-not-found", actual);
    }

    [Fact]
    public void AnalysisClassesCannotBeSerializedDirectly()
    {
        // This is the reason the JSON view classes exist.  FileInfo and
        // DirectoryInfo lead the serializer through Directory, Parent and Root,
        // and a root directory is its own Root, so it walks that chain until it
        // gives up and reports an object cycle.  The file has to be one that is
        // really there: for a path that isn't on disk the serializer trips over
        // FileInfo.Length first and the failure is a FileNotFoundException
        // instead.
        var realFile = new FileInfo(typeof(FindSolutionsOutputFixture).Assembly.Location);

        var analyses = new List<SolutionAnalysis>() { new SolutionAnalysis(realFile) };

        var actual = Assert.ThrowsAny<JsonException>(
            () => JsonSerializer.Serialize(analyses));

        WriteLine(actual.Message);

        Assert.Contains("object cycle", actual.Message);
    }

    [Fact]
    public void JsonOutputForProjectWithReferences()
    {
        var analysis = GetAnalysis();

        analysis.Projects[0].References.Add(new ReferenceAnalysis()
        {
            ReferenceType = "project-ref",
            ReferenceTarget = @"..\Core\Core.csproj",
            IsOutsideOfSolutionRoot = true
        });

        var actual = SystemUnderTest.FormatAsJson(
            SolutionAnalysisJson.FromAnalyses(new List<SolutionAnalysis>() { analysis }));

        WriteLine(actual);

        Assert.Contains("App.sln", actual);
        Assert.Contains("Web.csproj", actual);
        Assert.Contains("net10.0", actual);
        Assert.Contains("project-ref", actual);
        Assert.Contains("Core.csproj", actual);

        // and it has to be valid json that survives a round trip
        var roundTripped = JsonSerializer.Deserialize<List<SolutionAnalysisJson>>(actual);

        Assert.NotNull(roundTripped);
        AssertThat.AreEqual(1, roundTripped!.Count, "Wrong solution count.");
        AssertThat.AreEqual(1, roundTripped[0].Projects.Count, "Wrong project count.");
        AssertThat.AreEqual(
            1, roundTripped[0].Projects[0].References.Count, "Wrong reference count.");
        AssertThat.AreEqual(
            "App.sln", roundTripped[0].SolutionFileName, "Wrong solution file name.");
        AssertThat.AreEqual(
            true,
            roundTripped[0].Projects[0].References[0].IsOutsideOfSolutionRoot,
            "Wrong value for IsOutsideOfSolutionRoot.");
    }

    [Fact]
    public void JsonOutputForProjectThatIsNotOnDisk()
    {
        // FileInfo.Length throws for a file that isn't there, so a solution
        // that lists a missing project is a second way the old code could fail.
        var analysis = GetAnalysis();

        analysis.Projects[0].Exists = false;

        var actual = SystemUnderTest.FormatAsJson(
            SolutionAnalysisJson.FromAnalyses(new List<SolutionAnalysis>() { analysis }));

        WriteLine(actual);

        Assert.Contains("Web.csproj", actual);
        Assert.Contains("\"Exists\": false", actual);
    }

    [Fact]
    public void JsonOutputForSolutionsWithoutProjects()
    {
        // findsolutions --json without --listprojects
        var solutions = new[]
        {
            PathFor("src", "App", "App.sln"),
            PathFor("src", "Other", "Other.slnx")
        };

        var actual = SystemUnderTest.FormatAsJson(SolutionFileJson.FromPaths(solutions));

        WriteLine(actual);

        var roundTripped = JsonSerializer.Deserialize<List<SolutionFileJson>>(actual);

        Assert.NotNull(roundTripped);
        AssertThat.AreEqual(2, roundTripped!.Count, "Wrong solution count.");
        AssertThat.AreEqual(
            "App.sln", roundTripped[0].SolutionFileName, "Wrong solution file name.");
        AssertThat.AreEqual(
            "App", roundTripped[0].SolutionDirectory, "Wrong solution directory name.");
    }
}
