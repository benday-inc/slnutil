using Benday.Common.Testing;
using Benday.SolutionUtil.Api.GitHubActions;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class GitHubActionInfoFixture : TestClassBase
{
    public GitHubActionInfoFixture(ITestOutputHelper output) : base(output)
    {

    }

    private readonly GitHubActionInfo? _SystemUnderTest;

    private GitHubActionInfo SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                // _SystemUnderTest = new GitHubActionInfo();
                AssertThat.Fail("System under test was not initialized");
            }

            return _SystemUnderTest;
        }
    }

    [Fact]
    public void Constructor_InitializesPropertiesFromString()
    {
        // arrange
        var input = "actions/setup-dotnet@v5";

        var expected = new GitHubActionInfo()
        {
            Owner = "actions",
            Name = "setup-dotnet",
            Version = "v5"
        };

        // act
        var actual = new GitHubActionInfo(input);

        // assert
        AssertThat.AreEqual(expected.Owner, actual.Owner);
        AssertThat.AreEqual(expected.Name, actual.Name);
        AssertThat.AreEqual(expected.Version, actual.Version);

        var actualString = actual.ToString();
        AssertThat.AreEqual(input, actualString);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // arrange
        var first = new GitHubActionInfo("actions/setup-dotnet@v5");
        var second = new GitHubActionInfo("actions/setup-dotnet@v5");

        // act
        var actual = first.Equals(second);

        // assert
        AssertThat.AreEqual(true, actual);
    }

    [Fact]
    public void Equals_SameValuesDifferentCase_ReturnsTrue()
    {
        // arrange
        var first = new GitHubActionInfo("Actions/Setup-Dotnet@V5");
        var second = new GitHubActionInfo("actions/setup-dotnet@v5");

        // act
        var actual = first.Equals(second);

        // assert
        AssertThat.AreEqual(true, actual);
    }

    [Fact]
    public void Equals_DifferentOwner_ReturnsFalse()
    {
        // arrange
        var first = new GitHubActionInfo("actions/setup-dotnet@v5");
        var second = new GitHubActionInfo("microsoft/setup-dotnet@v5");

        // act
        var actual = first.Equals(second);

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void Equals_DifferentName_ReturnsFalse()
    {
        // arrange
        var first = new GitHubActionInfo("actions/setup-dotnet@v5");
        var second = new GitHubActionInfo("actions/setup-node@v5");

        // act
        var actual = first.Equals(second);

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void Equals_DifferentVersion_ReturnsFalse()
    {
        // arrange
        var first = new GitHubActionInfo("actions/setup-dotnet@v5");
        var second = new GitHubActionInfo("actions/setup-dotnet@v4");

        // act
        var actual = first.Equals(second);

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // arrange
        var first = new GitHubActionInfo("actions/setup-dotnet@v5");

        // act
        var actual = first.Equals(null);

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // arrange
        var first = new GitHubActionInfo("actions/setup-dotnet@v5");

        // act
        var actual = first.Equals("actions/setup-dotnet@v5");

        // assert
        AssertThat.AreEqual(false, actual);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        // arrange
        var first = new GitHubActionInfo("actions/setup-dotnet@v5");
        var second = new GitHubActionInfo("actions/setup-dotnet@v5");

        // act & assert
        AssertThat.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameValuesDifferentCase_ReturnsSameHash()
    {
        // arrange
        var first = new GitHubActionInfo("Actions/Setup-Dotnet@V5");
        var second = new GitHubActionInfo("actions/setup-dotnet@v5");

        // act & assert
        AssertThat.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void VersionType_MajorTag_SingleDigit()
    {
        // arrange & act
        var actual = new GitHubActionInfo("actions/setup-dotnet@v5");

        // assert
        AssertThat.AreEqual(GitHubActionVersionType.MajorTag, actual.VersionType);
    }

    [Fact]
    public void VersionType_MajorTag_MultipleDigits()
    {
        // arrange & act
        var actual = new GitHubActionInfo("actions/setup-dotnet@v12");

        // assert
        AssertThat.AreEqual(GitHubActionVersionType.MajorTag, actual.VersionType);
    }

    [Fact]
    public void VersionType_SpecificTag_TwoPart()
    {
        // arrange & act
        var actual = new GitHubActionInfo("actions/setup-dotnet@v5.1");

        // assert
        AssertThat.AreEqual(GitHubActionVersionType.SpecificTag, actual.VersionType);
    }

    [Fact]
    public void VersionType_SpecificTag_ThreePart()
    {
        // arrange & act
        var actual = new GitHubActionInfo("actions/setup-dotnet@v5.1.6");

        // assert
        AssertThat.AreEqual(GitHubActionVersionType.SpecificTag, actual.VersionType);
    }

    [Fact]
    public void VersionType_Sha_Short()
    {
        // arrange & act
        var actual = new GitHubActionInfo("actions/setup-dotnet@a5ac7e5");

        // assert
        AssertThat.AreEqual(GitHubActionVersionType.Sha, actual.VersionType);
    }

    [Fact]
    public void VersionType_Sha_Full()
    {
        // arrange & act
        var actual = new GitHubActionInfo("actions/setup-dotnet@a5ac7e51b28d7a44b5addd5ef1ef69cbfb6e4090");

        // assert
        AssertThat.AreEqual(GitHubActionVersionType.Sha, actual.VersionType);
    }

    [Fact]
    public void VersionType_Branch_Main()
    {
        // arrange & act
        var actual = new GitHubActionInfo("actions/setup-dotnet@main");

        // assert
        AssertThat.AreEqual(GitHubActionVersionType.Branch, actual.VersionType);
    }

    [Fact]
    public void VersionType_Branch_Develop()
    {
        // arrange & act
        var actual = new GitHubActionInfo("actions/setup-dotnet@develop");

        // assert
        AssertThat.AreEqual(GitHubActionVersionType.Branch, actual.VersionType);
    }

    [Fact]
    public void VersionType_DefaultConstructor_IsUnknown()
    {
        // arrange & act
        var actual = new GitHubActionInfo();

        // assert
        AssertThat.AreEqual(GitHubActionVersionType.Unknown, actual.VersionType);
    }
}