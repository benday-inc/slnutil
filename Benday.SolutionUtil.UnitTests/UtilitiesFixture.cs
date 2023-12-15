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

}