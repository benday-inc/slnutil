using Benday.SolutionUtil.Api;

namespace Benday.SolutionUtil.UnitTests;

[TestClass]
public class UtilitiesFixture
{
    // create a data driven test
    [TestMethod]
    [DataRow("Abcdef", "Abcddd", 4)]
    [DataRow("Abcdef", "Abcdef", 6)]
    [DataRow("Abcdef", "Abcdefgh", 6)]
    [DataRow("Abcdef", "Abcd", 4)]
    [DataRow("Abcdef", "Abc", 3)]
    [DataRow("qwertqwer", "Abcddd", 0)]
    [DataRow("qwertqwer", "", 0)]
    [DataRow("Benday.BuildDeployDemo.WebUi.dll", "Benday.BuildDeployDemo.Api.dll", 23)]
    public void FindNumberOfMatchingCharacters(string originalFileName, string fileName, int expectedMatchCount)
    {
        // arrange
        // act
        var actual = 
            Utilities.FindNumberOfMatchingCharacters(
                originalFileName, fileName);

        // assert
        Assert.AreEqual(
            expectedMatchCount, 
            actual, 
            "FindNumberOfMatchingCharacters() returned wrong value.");
    }

    [TestMethod]
    [DataRow("voice_id", "VoiceId")]
    [DataRow("settings", "Settings")]
    [DataRow("high_quality_base_model_ids", "HighQualityBaseModelIds")]
    [DataRow("TestValue", "TestValue")]
    [DataRow("use case", "UseCase")]
    [DataRow("testy-thingy", "TestyThingy")]
    public void JsonNameToCsharpName(string input, string expected)
    {
        // arrange

        // act
        var actual =
            Utilities.JsonNameToCsharpName(input);

        // assert
        Assert.AreEqual(
            expected,
            actual,
            "JsonNameToCsharpName() returned wrong value.");
    }

    [TestMethod]
    [DataRow("8.1.2", "8.*")]
    [DataRow("18.1.2", "18.*")]
    [DataRow("18.*.*", "18.*")]
    [DataRow("182.*", "182.*")]
    [DataRow("182", "182")]
    public void PackageVersionNumberToWildcard(string input, string expected)
    {
        // arrange

        // act
        var actual =
            Utilities.PackageVersionNumberToWildcard(input);

        // assert
        Assert.AreEqual(
            expected,
            actual,
            "PackageVersionNumberToWildcard() returned wrong value.");
    }

    [TestMethod]
    [DataRow("net7.0", "net8.0", "net8.0")]
    [DataRow("net9.0", "net9.0", "net9.0")]
    [DataRow("net8.0", "net9.0", "net9.0")]
    [DataRow("net7.0-windowsAsdf", "net8.0", "net8.0-windowsAsdf")]
    [DataRow(" net7.0 ", " netstandard2.1 ", "netstandard2.1")]
    [DataRow("net7.0", " net8.0", "net8.0")]
    [DataRow("net9.0", " net9.0", "net9.0")]
    [DataRow("net8.0 ", " net9.0", "net9.0")]
    [DataRow("net7.0-windowsAsdf ", " net8.0", "net8.0-windowsAsdf")]
    [DataRow(" net7.0 ", " netstandard2.1 ", "netstandard2.1")]
    [DataRow("net7.0-windowsAsdf ", " net9.0-windowsBingBong", "net9.0-windowsBingBong")]
    [DataRow("net7.0", " net9.0-windowsBingBong", "net9.0-windowsBingBong")]
    public void GetFrameworkVersion(string currentVersion, string targetVersion, string expectedValue)
    {
        // arrange

        // act
        var actual =
            Utilities.GetFrameworkVersion(currentVersion, targetVersion);

        // assert
        Assert.AreEqual(
            expectedValue,
            actual,
            "GetFrameworkVersion() returned wrong value.");
    }

}