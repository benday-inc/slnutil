using System;

using Benday.Common.Testing;
using Benday.SolutionUtil.Api.GitHubActions;

using Xunit;

using Xunit.Abstractions;

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

        // act
        var actual = SystemUnderTest.GetAllActions();

        // assert

    }

}
