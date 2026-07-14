using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Benday.SolutionUtil.Api.Mcp;

/// <summary>
/// The MCP-capable clients slnutil knows how to generate configuration for.
/// </summary>
public enum McpClientType
{
    ClaudeCode,
    ClaudeDesktop,
    VisualStudioCode,
    VisualStudio,
    Cursor
}

/// <summary>
/// Pure builders for MCP client configuration. Given the command used to launch
/// the slnutil server, these produce the JSON snippet a client expects and the
/// CLI arguments for clients that register servers via their own command line
/// (Claude Code's <c>claude mcp add</c>, VS Code's <c>code --add-mcp</c>).
///
/// Everything here is deterministic and side-effect free so it can be unit
/// tested without touching the filesystem or any client.
/// </summary>
public static class McpClientSetup
{
    /// <summary>
    /// The default launch command: the globally-installed <c>slnutil</c> tool
    /// invoked with the <c>mcp-server</c> sub-command.
    /// </summary>
    public const string DefaultCommand = "slnutil";

    public const string ServerSubCommand = Constants.CommandArgumentNameMcpServer;

    /// <summary>
    /// Build the JSON configuration block for the given client. The shape differs
    /// per client: some nest servers under <c>mcpServers</c>, VS Code/Visual
    /// Studio use <c>servers</c>, and the entry is keyed by the server name.
    /// </summary>
    public static string BuildConfigJson(
        McpClientType clientType,
        string serverName = McpServerConstants.ServerName,
        string command = DefaultCommand)
    {
        var serverEntry = new JsonObject
        {
            ["command"] = command,
            ["args"] = new JsonArray(ServerSubCommand)
        };

        // VS Code and Visual Studio expect a "type" discriminator for stdio servers.
        if (clientType is McpClientType.VisualStudioCode or McpClientType.VisualStudio)
        {
            serverEntry.Insert(0, "type", "stdio");
        }

        var containerKey = GetServerContainerKey(clientType);

        var root = new JsonObject
        {
            [containerKey] = new JsonObject
            {
                [serverName] = serverEntry
            }
        };

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// The top-level JSON key under which a client lists its MCP servers.
    /// </summary>
    public static string GetServerContainerKey(McpClientType clientType)
    {
        return clientType switch
        {
            McpClientType.VisualStudioCode => "servers",
            McpClientType.VisualStudio => "servers",
            _ => "mcpServers"
        };
    }

    /// <summary>
    /// Build the CLI arguments that register the server at user scope for clients
    /// that support command-line registration. Returns null for clients that only
    /// support file-based configuration.
    /// </summary>
    /// <returns>
    /// The full argument list including the client executable name (e.g.
    /// <c>claude mcp add slnutil -- slnutil mcp-server</c>), or null.
    /// </returns>
    public static IReadOnlyList<string>? BuildInstallCliArgs(
        McpClientType clientType,
        string serverName = McpServerConstants.ServerName,
        string command = DefaultCommand)
    {
        return clientType switch
        {
            McpClientType.ClaudeCode => new[]
            {
                "claude", "mcp", "add", serverName, "--", command, ServerSubCommand
            },
            McpClientType.VisualStudioCode => new[]
            {
                "code", "--add-mcp",
                BuildVsCodeAddMcpPayload(serverName, command)
            },
            _ => null
        };
    }

    /// <summary>
    /// Build the CLI arguments that remove the server at user scope, or null for
    /// clients that only support file-based configuration.
    /// </summary>
    public static IReadOnlyList<string>? BuildUninstallCliArgs(
        McpClientType clientType,
        string serverName = McpServerConstants.ServerName)
    {
        return clientType switch
        {
            McpClientType.ClaudeCode => new[]
            {
                "claude", "mcp", "remove", serverName
            },
            _ => null
        };
    }

    /// <summary>
    /// The single-line JSON payload that <c>code --add-mcp</c> expects.
    /// </summary>
    public static string BuildVsCodeAddMcpPayload(
        string serverName = McpServerConstants.ServerName,
        string command = DefaultCommand)
    {
        var payload = new JsonObject
        {
            ["name"] = serverName,
            ["type"] = "stdio",
            ["command"] = command,
            ["args"] = new JsonArray(ServerSubCommand)
        };

        return payload.ToJsonString();
    }

    public static string GetClientDisplayName(McpClientType clientType)
    {
        return clientType switch
        {
            McpClientType.ClaudeCode => "Claude Code",
            McpClientType.ClaudeDesktop => "Claude Desktop",
            McpClientType.VisualStudioCode => "Visual Studio Code",
            McpClientType.VisualStudio => "Visual Studio 2022 / 2026",
            McpClientType.Cursor => "Cursor",
            _ => clientType.ToString()
        };
    }

    /// <summary>
    /// Where the client stores its MCP configuration, as human-readable guidance.
    /// </summary>
    public static string GetConfigLocationHint(McpClientType clientType)
    {
        return clientType switch
        {
            McpClientType.ClaudeCode =>
                "Registered via 'claude mcp add', or add to .mcp.json in your project root.",
            McpClientType.ClaudeDesktop =>
                "Add to claude_desktop_config.json (Settings > Developer > Edit Config).",
            McpClientType.VisualStudioCode =>
                "Add to .vscode/mcp.json in your workspace, or run 'code --add-mcp'.",
            McpClientType.VisualStudio =>
                "Add to a .mcp.json file in your solution directory (%USERPROFILE%\\.mcp.json for global).",
            McpClientType.Cursor =>
                "Add to .cursor/mcp.json in your project, or ~/.cursor/mcp.json for global.",
            _ => string.Empty
        };
    }
}
