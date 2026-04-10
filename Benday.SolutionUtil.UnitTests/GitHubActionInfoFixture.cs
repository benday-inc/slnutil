using Benday.Common.Testing;
using Benday.SolutionUtil.Api.GitHubActions;

using Xunit;

using Xunit.Abstractions;

namespace Benday.SolutionUtil.UnitTests;

public class GitHubActionInfoFixture : TestClassBase
{
    public GitHubActionInfoFixture(ITestOutputHelper output) : base(output)
    {

    }

    private GitHubActionInfo? _SystemUnderTest;

    private GitHubActionInfo SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new GitHubActionInfo();
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
}