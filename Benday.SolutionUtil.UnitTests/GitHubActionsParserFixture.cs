using System;

using Benday.Common.Testing;
using Benday.SolutionUtil.Api.GitHubActions;

using Moq;

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

    [Fact]
    public void GetAllActionsWithLatestInfo()
    {
        // arrange
        var testYaml = base.GetSampleFileText("github-actions-sample.yml");

        AssertThatString.IsNotNullOrEmpty(testYaml, "Test YAML file is null or empty.");

        var result = MockUtility.Build<GitHubActionsParser>()
            .UsingConstructor(typeof(string), typeof(IGitHubActionsInfoProvider))
            .WithValue(testYaml)
            .Build();

        var mock = result.GetRequiredMock<IGitHubActionsInfoProvider>();

        mock.Setup(m => m.GetLatestActionInfo(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((ownerName, actionName) =>
                    {
                        var versionNumber = actionName switch
                        {
                            "checkout" => "6",
                            "setup-dotnet" => "5",
                            "upload-artifact" => "6",
                            "download-artifact" => "4",
                            "login" => "2",
                            "webapps-deploy" => "3",
                            _ => throw new InvalidOperationException($"Unexpected action name: {actionName}")
                        };

                        return new GitHubActionInfo($"{ownerName}/{actionName}@v{versionNumber}");
                    });

        _SystemUnderTest = result.Instance;

        var expectedInitialValues = new GitHubActionInfo[]
        {
            new("actions/checkout@v6"),
            new("actions/setup-dotnet@v5"),
            new("actions/upload-artifact@v6"),
            new("actions/download-artifact@v4"),
            new("azure/login@v2"),
            new("azure/webapps-deploy@v3")
        };

        var expected = ConvertToVersionInfo(expectedInitialValues);

        // act
        var actual = SystemUnderTest.GetAllActionsWithLatestInfo();

        // assert
        AssertThat.IsNotNull(actual, "GetAllActionsWithLatestInfo() returned null.");
        AssertThat.AreEqual(expected.Length, actual.Length, "GetAllActionsWithLatestInfo() returned wrong number of actions.");

        AssertAreEqual(expected, actual);

    }

    [Fact]
    public void GetAllActionsThatNeedUpdates()
    {
        // arrange
        var testYaml = base.GetSampleFileText("github-actions-sample.yml");

        AssertThatString.IsNotNullOrEmpty(testYaml, "Test YAML file is null or empty.");

        var result = MockUtility.Build<GitHubActionsParser>()
            .UsingConstructor(typeof(string), typeof(IGitHubActionsInfoProvider))
            .WithValue(testYaml)
            .Build();

        var mock = result.GetRequiredMock<IGitHubActionsInfoProvider>();

        mock.Setup(m => m.GetLatestActionInfo(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((ownerName, actionName) =>
                    {
                        var versionNumber = actionName switch
                        {
                            "checkout" => "7",
                            "setup-dotnet" => "5",
                            "upload-artifact" => "6",
                            "download-artifact" => "4",
                            "login" => "4",
                            "webapps-deploy" => "3",
                            _ => throw new InvalidOperationException($"Unexpected action name: {actionName}")
                        };

                        return new GitHubActionInfo($"{ownerName}/{actionName}@v{versionNumber}");
                    });

        _SystemUnderTest = result.Instance;

        var expected = new GitHubActionVersionInfo[]
        {
            new(new GitHubActionInfo("actions/checkout@v6"), new GitHubActionInfo("actions/checkout@v7")),
            new(new GitHubActionInfo("azure/login@v2"), new GitHubActionInfo("azure/login@v4"))
        };

        // act
        var actual = SystemUnderTest.GetAllActionsThatNeedUpdates();

        // assert
        AssertThat.IsNotNull(actual, "GetAllActionsThatNeedUpdates() returned null.");
        AssertThat.AreEqual(2, actual.Length, "GetAllActionsThatNeedUpdates() returned wrong number of actions that need updates.");
        AssertThat.AreEqual(expected.Length, actual.Length, "GetAllActionsThatNeedUpdates() returned wrong number of actions.");

        AssertAreEqual(expected, actual);
    }

    private GitHubActionVersionInfo[] ConvertToVersionInfo(GitHubActionInfo[] expectedInitialValues)
    {
        var result = new GitHubActionVersionInfo[expectedInitialValues.Length];

        for (int i = 0; i < expectedInitialValues.Length; i++)
        {
            var actionInfo = expectedInitialValues[i];
            result[i] = new GitHubActionVersionInfo(actionInfo, actionInfo);
        }

        return result;
    }


    private void AssertAreEqual(GitHubActionVersionInfo[] expected, GitHubActionVersionInfo[] actual)
    {
        // sort both arrays by the string representation of the action info, so that we can compare them regardless of the order they were returned in.
        var expectedSorted = expected.OrderBy(e => e.Current.ToString()).ToArray();
        var actualSorted = actual.OrderBy(a => a.Current.ToString()).ToArray();

        for (int i = 0; i < expectedSorted.Length; i++)
        {
            var expectedAction = expectedSorted[i];
            var actualAction = actualSorted[i];

            AssertThat.AreEqual(expectedAction.Current.ToString(), actualAction.Current.ToString(), $"Current action at index {i} does not match.");
            AssertThat.AreEqual(expectedAction.Latest?.ToString(), actualAction.Latest?.ToString(), $"Latest action at index {i} does not match.");
        }        
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
