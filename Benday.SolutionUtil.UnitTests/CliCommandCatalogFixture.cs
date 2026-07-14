using Benday.Common.Testing;
using Benday.SolutionUtil.Api;
using Benday.SolutionUtil.Api.Mcp.Tools;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class CliCommandCatalogFixture : TestClassBase
{
    public CliCommandCatalogFixture(ITestOutputHelper output) : base(output)
    {
    }

    private static IReadOnlyList<CliCommandDescriptor> GetCommands()
    {
        return CliCommandCatalog.GetCommands(typeof(Constants).Assembly);
    }

    [Fact]
    public void GetCommands_ReturnsCommands()
    {
        // act
        var commands = GetCommands();

        // assert
        Assert.NotEmpty(commands);
    }

    [Theory]
    [InlineData("base64")]
    [InlineData("listsolutionprojects")]
    [InlineData("findsolutions")]
    [InlineData("runsql")]
    [InlineData("devtreeclean")]
    [InlineData("deployefmigrations")]
    public void GetCommands_IncludesKnownCommand(string commandName)
    {
        // act
        var commands = GetCommands();

        // assert
        Assert.Contains(commands, c => c.Name == commandName);
    }

    [Fact]
    public void GetCommands_MapsBase64ToMcpTool()
    {
        // act
        var command = GetCommands().Single(c => c.Name == "base64");

        // assert
        Assert.Equal("base64_encode", command.McpToolName);
    }

    [Theory]
    [InlineData("runsql")]
    [InlineData("devtreeclean")]
    [InlineData("deployefmigrations")]
    public void GetCommands_DangerousCommands_AreNotExposedAsTools(string commandName)
    {
        // act
        var command = GetCommands().Single(c => c.Name == commandName);

        // assert
        Assert.Null(command.McpToolName);
    }

    [Fact]
    public void GetCommands_RequiredArgs_AppearInExample()
    {
        // act -- listsolutionprojects has an optional solution path, base64 has a required value
        var base64 = GetCommands().Single(c => c.Name == "base64");

        // assert
        Assert.Contains("base64", base64.Example);
        Assert.Contains("/value:", base64.Example);
    }

    [Fact]
    public void GetCommands_EveryMappedToolName_HasMatchingCommand()
    {
        // arrange
        var commandNames = GetCommands().Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // assert -- the coverage map must not reference commands that don't exist
        foreach (var mappedCommand in CliCommandCatalog.CommandToMcpTool.Keys)
        {
            Assert.Contains(mappedCommand, commandNames);
        }
    }

    [Fact]
    public void GetCommands_Arguments_HaveNames()
    {
        // act
        var command = GetCommands().Single(c => c.Name == "base64");

        // assert
        Assert.All(command.Arguments, arg => Assert.False(string.IsNullOrWhiteSpace(arg.Name)));
    }
}
