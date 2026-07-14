using System.Text.Json;

using Benday.Common.Testing;
using Benday.SolutionUtil.Api.Mcp;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class McpClientSetupFixture : TestClassBase
{
    public McpClientSetupFixture(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(McpClientType.ClaudeCode, "mcpServers")]
    [InlineData(McpClientType.ClaudeDesktop, "mcpServers")]
    [InlineData(McpClientType.Cursor, "mcpServers")]
    [InlineData(McpClientType.VisualStudioCode, "servers")]
    [InlineData(McpClientType.VisualStudio, "servers")]
    public void BuildConfigJson_UsesExpectedContainerKey(McpClientType clientType, string expectedKey)
    {
        // act
        var json = McpClientSetup.BuildConfigJson(clientType);

        // assert
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty(expectedKey, out var container),
            $"Expected container key '{expectedKey}'.");
        Assert.True(container.TryGetProperty("slnutil", out _),
            "Expected the server to be keyed by its name 'slnutil'.");
    }

    [Fact]
    public void BuildConfigJson_ServerEntry_HasCommandAndArgs()
    {
        // act
        var json = McpClientSetup.BuildConfigJson(McpClientType.ClaudeCode);

        // assert
        using var doc = JsonDocument.Parse(json);
        var server = doc.RootElement.GetProperty("mcpServers").GetProperty("slnutil");

        Assert.Equal("slnutil", server.GetProperty("command").GetString());

        var args = server.GetProperty("args").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(new[] { "mcp-server" }, args);
    }

    [Theory]
    [InlineData(McpClientType.VisualStudioCode)]
    [InlineData(McpClientType.VisualStudio)]
    public void BuildConfigJson_VsClients_IncludeStdioType(McpClientType clientType)
    {
        // act
        var json = McpClientSetup.BuildConfigJson(clientType);

        // assert
        using var doc = JsonDocument.Parse(json);
        var container = doc.RootElement.GetProperty("servers").GetProperty("slnutil");
        Assert.Equal("stdio", container.GetProperty("type").GetString());
    }

    [Fact]
    public void BuildConfigJson_NonVsClients_OmitStdioType()
    {
        // act
        var json = McpClientSetup.BuildConfigJson(McpClientType.ClaudeCode);

        // assert
        using var doc = JsonDocument.Parse(json);
        var server = doc.RootElement.GetProperty("mcpServers").GetProperty("slnutil");
        Assert.False(server.TryGetProperty("type", out _));
    }

    [Fact]
    public void BuildInstallCliArgs_ClaudeCode_UsesClaudeMcpAdd()
    {
        // act
        var args = McpClientSetup.BuildInstallCliArgs(McpClientType.ClaudeCode);

        // assert
        Assert.NotNull(args);
        Assert.Equal(new[] { "claude", "mcp", "add", "slnutil", "--", "slnutil", "mcp-server" }, args);
    }

    [Fact]
    public void BuildInstallCliArgs_VsCode_UsesCodeAddMcp()
    {
        // act
        var args = McpClientSetup.BuildInstallCliArgs(McpClientType.VisualStudioCode);

        // assert
        Assert.NotNull(args);
        Assert.Equal("code", args![0]);
        Assert.Equal("--add-mcp", args[1]);

        // the payload is valid JSON describing the server
        using var doc = JsonDocument.Parse(args[2]);
        Assert.Equal("slnutil", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("stdio", doc.RootElement.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(McpClientType.ClaudeDesktop)]
    [InlineData(McpClientType.VisualStudio)]
    [InlineData(McpClientType.Cursor)]
    public void BuildInstallCliArgs_FileOnlyClients_ReturnNull(McpClientType clientType)
    {
        // act
        var args = McpClientSetup.BuildInstallCliArgs(clientType);

        // assert
        Assert.Null(args);
    }

    [Fact]
    public void BuildUninstallCliArgs_ClaudeCode_UsesRemove()
    {
        // act
        var args = McpClientSetup.BuildUninstallCliArgs(McpClientType.ClaudeCode);

        // assert
        Assert.NotNull(args);
        Assert.Equal(new[] { "claude", "mcp", "remove", "slnutil" }, args);
    }

    [Fact]
    public void BuildConfigJson_CustomCommand_IsHonored()
    {
        // act
        var json = McpClientSetup.BuildConfigJson(
            McpClientType.ClaudeCode, command: "/usr/local/bin/slnutil");

        // assert
        using var doc = JsonDocument.Parse(json);
        var server = doc.RootElement.GetProperty("mcpServers").GetProperty("slnutil");
        Assert.Equal("/usr/local/bin/slnutil", server.GetProperty("command").GetString());
    }
}
