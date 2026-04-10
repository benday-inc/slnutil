using System;

using Benday.Common.Testing;
using Benday.SolutionUtil.Api.GitHubActions;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class GitHubActionsParserFixture : TestClassBase
{
    public GitHubActionsParserFixture(ITestOutputHelper output) : base(output)
    {

    }

    private GitHubActionsParser? _SystemUnderTest;

    private GitHubActionsParser SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                AssertThat.Fail("System under test was not initialized");
            }

            return _SystemUnderTest;
        }
    }

    [Fact]
    public void LoadYaml()
    {
        var testYaml = base.GetSampleFileText("github-actions-sample.yml");

        _SystemUnderTest = new GitHubActionsParser(testYaml);

        // if this didn't throw an exception, then we are good. We will add more tests later to verify the content of the YAML file is parsed correctly.
    }

    [Fact]
    public void GetAllActions()
    {
        // arrange
        var testYaml = base.GetSampleFileText("github-actions-sample.yml");
        _SystemUnderTest = new GitHubActionsParser(testYaml);

        var expected = new GitHubActionInfo[]
        {
            new("actions/checkout@v6"),
            new("actions/setup-dotnet@v5"),
            new("actions/upload-artifact@v6"),
            new("actions/download-artifact@v4"),
            new("azure/login@v2"),
            new("azure/webapps-deploy@v3")
        };

        // act
        var actual = SystemUnderTest.GetAllActions();
        
        // assert
        AssertThat.IsNotNull(actual, "GetAllActions() returned null.");        
        AssertThat.AreEqual(expected.Length, actual.Length, "GetAllActions() returned wrong number of actions.");

        AssertAreEqual(expected, actual);

    }

    private void AssertAreEqual(GitHubActionInfo[] expected, GitHubActionInfo[] actual)
    {
        // sort both arrays by the string representation of the action info, so that we can compare them regardless of the order they were returned in.
        var expectedSorted = expected.OrderBy(e => e.ToString()).ToArray();
        var actualSorted = actual.OrderBy(a => a.ToString()).ToArray();

        for (int i = 0; i < expectedSorted.Length; i++)
        {
            var expectedAction = expectedSorted[i];
            var actualAction = actualSorted[i];

            AssertThat.AreEqual(expectedAction.ToString(), actualAction.ToString(), $"Action at index {i} does not match.");
        }
    }
}
