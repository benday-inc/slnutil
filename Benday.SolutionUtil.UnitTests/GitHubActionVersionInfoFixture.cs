using Benday.Common.Testing;
using Benday.SolutionUtil.Api.GitHubActions;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class GitHubActionVersionInfoFixture : TestClassBase
{
    public GitHubActionVersionInfoFixture(ITestOutputHelper output) : base(output)
    {

    }

    [Fact]
    public void NeedsUpgrade_MajorTag_NewerVersionAvailable_ReturnsTrue()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@v4");
        var latest = new GitHubActionInfo("actions/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade();

        // assert
        AssertThat.AreEqual(true, actual);
    }

    [Fact]
    public void NeedsUpgrade_MajorTag_AlreadyOnLatest_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@v5");
        var latest = new GitHubActionInfo("actions/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade();

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void NeedsUpgrade_MajorTag_CurrentIsNewer_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@v6");
        var latest = new GitHubActionInfo("actions/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade();

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void NeedsUpgrade_LatestIsNull_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@v4");
        var systemUnderTest = new GitHubActionVersionInfo(current, null);

        // act
        var actual = systemUnderTest.NeedsUpgrade();

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void NeedsUpgrade_DifferentOwner_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@v4");
        var latest = new GitHubActionInfo("microsoft/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade();

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void NeedsUpgrade_DifferentName_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@v4");
        var latest = new GitHubActionInfo("actions/setup-node@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade();

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void NeedsUpgrade_SpecificTag_WithoutCleanup_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@v4.1.2");
        var latest = new GitHubActionInfo("actions/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade();

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void NeedsUpgrade_SpecificTag_WithCleanup_NewerAvailable_ReturnsTrue()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@v4.1.2");
        var latest = new GitHubActionInfo("actions/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade(cleanup: true);

        // assert
        AssertThat.AreEqual(true, actual);
    }

    [Fact]
    public void NeedsUpgrade_SpecificTag_WithCleanup_SameMajor_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@v5.1.2");
        var latest = new GitHubActionInfo("actions/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade(cleanup: true);

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void NeedsUpgrade_Branch_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@main");
        var latest = new GitHubActionInfo("actions/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade();

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void NeedsUpgrade_Sha_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@a5ac7e5");
        var latest = new GitHubActionInfo("actions/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade();

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void NeedsUpgrade_Branch_WithCleanup_ReturnsFalse()
    {
        // arrange
        var current = new GitHubActionInfo("actions/setup-dotnet@main");
        var latest = new GitHubActionInfo("actions/setup-dotnet@v5");
        var systemUnderTest = new GitHubActionVersionInfo(current, latest);

        // act
        var actual = systemUnderTest.NeedsUpgrade(cleanup: true);

        // assert
        AssertThat.AreEqual(false, actual);
    }
}
