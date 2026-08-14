using Benday.Common.Testing;
using Benday.SolutionUtil.Api;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class SolutionAnalyzerFixture : TestClassBase
{
    public SolutionAnalyzerFixture(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// An absolute path for the platform the tests are running on, built from
    /// the segments given.
    /// </summary>
    private static string PathFor(params string[] segments)
    {
        var root = OperatingSystem.IsWindows() == true ? @"C:\" : "/";

        return Path.Combine(root, Path.Combine(segments));
    }

    [Fact]
    public void ResolveReferencePath_HandlesWindowsStyleRelativePaths()
    {
        // Project files are written on Windows and use backslashes.  On macOS
        // and Linux a backslash is an ordinary filename character, so without
        // converting it first the ".." never gets resolved.
        var baseDirectory = PathFor("src", "App", "Web");

        var actual = SolutionAnalyzer.ResolveReferencePath(
            baseDirectory, @"..\Common\Common.csproj");

        var expected = PathFor("src", "App", "Common", "Common.csproj");

        AssertThat.AreEqual(expected, actual, "Wrong resolved reference path.");
    }

    [Fact]
    public void ResolveReferencePath_HandlesSeveralLevelsUp()
    {
        var baseDirectory = PathFor("src", "App", "Web", "Site");

        var actual = SolutionAnalyzer.ResolveReferencePath(
            baseDirectory, @"..\..\..\Shared\Common.csproj");

        var expected = PathFor("src", "Shared", "Common.csproj");

        AssertThat.AreEqual(expected, actual, "Wrong resolved reference path.");
    }

    [Fact]
    public void ResolveReferencePath_HandlesForwardSlashes()
    {
        var baseDirectory = PathFor("src", "App", "Web");

        var actual = SolutionAnalyzer.ResolveReferencePath(
            baseDirectory, "../Common/Common.csproj");

        var expected = PathFor("src", "App", "Common", "Common.csproj");

        AssertThat.AreEqual(expected, actual, "Wrong resolved reference path.");
    }

    [Fact]
    public void IsInsideDirectory_ChildIsInside()
    {
        var actual = SolutionAnalyzer.IsInsideDirectory(
            PathFor("src", "App", "Web", "Site.csproj"), PathFor("src", "App"));

        AssertThat.AreEqual(true, actual, "A child path is inside the directory.");
    }

    [Fact]
    public void IsInsideDirectory_SameDirectoryIsInside()
    {
        var actual = SolutionAnalyzer.IsInsideDirectory(
            PathFor("src", "App"), PathFor("src", "App"));

        AssertThat.AreEqual(true, actual, "A directory contains itself.");
    }

    [Fact]
    public void IsInsideDirectory_SiblingSharingANamePrefixIsNotInside()
    {
        // This is what a raw StartsWith comparison gets wrong: "App.Tests"
        // starts with "App", so a sibling reads as living inside its neighbour
        // and a genuine cross-solution reference goes unreported.
        var actual = SolutionAnalyzer.IsInsideDirectory(
            PathFor("src", "App.Tests", "Tests.csproj"), PathFor("src", "App"));

        AssertThat.AreEqual(false, actual, "A sibling is not inside its neighbour.");
    }

    [Fact]
    public void IsInsideDirectory_UnrelatedPathIsNotInside()
    {
        var actual = SolutionAnalyzer.IsInsideDirectory(
            PathFor("other", "Thing.csproj"), PathFor("src", "App"));

        AssertThat.AreEqual(false, actual, "An unrelated path is not inside the directory.");
    }

    [Fact]
    public void IsInsideDirectory_TrailingSeparatorOnTheDirectory()
    {
        var directory = PathFor("src", "App") + Path.DirectorySeparatorChar;

        var actual = SolutionAnalyzer.IsInsideDirectory(
            PathFor("src", "App", "Web", "Site.csproj"), directory);

        AssertThat.AreEqual(true, actual, "A trailing separator should not change the answer.");
    }

    [Fact]
    public void ResolvedReferenceOutsideTheSolutionIsDetected()
    {
        // The two fixes together: a Windows-style relative path that genuinely
        // leaves the solution folder has to resolve and then compare correctly.
        var projectDirectory = PathFor("src", "App", "Web");
        var solutionDirectory = PathFor("src", "App");

        var resolved = SolutionAnalyzer.ResolveReferencePath(
            projectDirectory, @"..\..\Shared\Common.csproj");

        var actual = SolutionAnalyzer.IsInsideDirectory(resolved, solutionDirectory);

        AssertThat.AreEqual(
            false, actual, "This reference leaves the solution folder and should be reported.");
    }

    [Fact]
    public void ResolvedReferenceInsideTheSolutionIsNotDetected()
    {
        var projectDirectory = PathFor("src", "App", "Web");
        var solutionDirectory = PathFor("src", "App");

        var resolved = SolutionAnalyzer.ResolveReferencePath(
            projectDirectory, @"..\Core\Core.csproj");

        var actual = SolutionAnalyzer.IsInsideDirectory(resolved, solutionDirectory);

        AssertThat.AreEqual(
            true, actual, "This reference stays inside the solution folder.");
    }
}
