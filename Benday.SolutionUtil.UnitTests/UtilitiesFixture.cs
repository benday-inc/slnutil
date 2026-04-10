using Benday.Common.Testing;
using Benday.SolutionUtil.Api;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class UtilitiesFixture : TestClassBase
{
    public UtilitiesFixture(ITestOutputHelper output) : base(output)
    {
    }

    // create a data driven test
    [Theory]
    [InlineData("Abcdef", "Abcddd", 4)]
    [InlineData("Abcdef", "Abcdef", 6)]
    [InlineData("Abcdef", "Abcdefgh", 6)]
    [InlineData("Abcdef", "Abcd", 4)]
    [InlineData("Abcdef", "Abc", 3)]
    [InlineData("qwertqwer", "Abcddd", 0)]
    [InlineData("qwertqwer", "", 0)]
    [InlineData("Benday.BuildDeployDemo.WebUi.dll", "Benday.BuildDeployDemo.Api.dll", 23)]
    public void FindNumberOfMatchingCharacters(string originalFileName, string fileName, int expectedMatchCount)
    {
        // arrange
        // act
        var actual =
            Utilities.FindNumberOfMatchingCharacters(
                originalFileName, fileName);

        // assert
        AssertThat.AreEqual(
            expectedMatchCount,
            actual,
            "FindNumberOfMatchingCharacters() returned wrong value.");
    }

    [Theory]
    [InlineData("voice_id", "VoiceId")]
    [InlineData("settings", "Settings")]
    [InlineData("high_quality_base_model_ids", "HighQualityBaseModelIds")]
    [InlineData("TestValue", "TestValue")]
    [InlineData("use case", "UseCase")]
    [InlineData("testy-thingy", "TestyThingy")]
    public void JsonNameToCsharpName(string input, string expected)
    {
        // arrange

        // act
        var actual =
            Utilities.JsonNameToCsharpName(input);

        // assert
        AssertThat.AreEqual(
            expected,
            actual,
            "JsonNameToCsharpName() returned wrong value.");
    }

    [Theory]
    [InlineData("8.1.2", "8.*")]
    [InlineData("18.1.2", "18.*")]
    [InlineData("18.*.*", "18.*")]
    [InlineData("182.*", "182.*")]
    [InlineData("182", "182")]
    public void PackageVersionNumberToWildcard(string input, string expected)
    {
        // arrange

        // act
        var actual =
            Utilities.PackageVersionNumberToWildcard(input);

        // assert
        AssertThat.AreEqual(
            expected,
            actual,
            "PackageVersionNumberToWildcard() returned wrong value.");
    }

    [Theory]
    [InlineData("net7.0", "net8.0", "net8.0")]
    [InlineData("net9.0", "net9.0", "net9.0")]
    [InlineData("net8.0", "net9.0", "net9.0")]
    [InlineData("net7.0-windowsAsdf", "net8.0", "net8.0-windowsAsdf")]
    [InlineData(" net7.0 ", " netstandard2.1 ", "netstandard2.1")]
    [InlineData("net7.0", " net8.0", "net8.0")]
    [InlineData("net9.0", " net9.0", "net9.0")]
    [InlineData("net8.0 ", " net9.0", "net9.0")]
    [InlineData("net7.0-windowsAsdf ", " net8.0", "net8.0-windowsAsdf")]
    [InlineData("net7.0-windowsAsdf ", " net9.0-windowsBingBong", "net9.0-windowsBingBong")]
    [InlineData("net7.0", " net9.0-windowsBingBong", "net9.0-windowsBingBong")]
    public void GetFrameworkVersion(string currentVersion, string targetVersion, string expectedValue)
    {
        // arrange

        // act
        var actual =
            Utilities.GetFrameworkVersion(currentVersion, targetVersion);

        // assert
        AssertThat.AreEqual(
            expectedValue,
            actual,
            "GetFrameworkVersion() returned wrong value.");
    }

}
