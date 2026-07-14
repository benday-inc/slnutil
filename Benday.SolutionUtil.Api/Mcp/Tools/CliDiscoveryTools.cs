using System.ComponentModel;
using System.Text.Json;

using ModelContextProtocol.Server;

namespace Benday.SolutionUtil.Api.Mcp.Tools;

/// <summary>
/// MCP tool that lists every slnutil command-line command. This is the fallback
/// for capabilities that are not exposed as dedicated MCP tools (including
/// write and destructive commands, which are intentionally CLI-only), so the AI
/// can show the user the right terminal command to run.
/// </summary>
[McpServerToolType]
public class CliDiscoveryTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [McpServerTool(Name = "discover_cli_commands", ReadOnly = true),
        Description(
            "List all slnutil command-line commands with their descriptions, arguments, " +
            "and an example invocation. Use this when the user needs a slnutil capability " +
            "that is not available as one of the other tools here — including commands that " +
            "modify files or a database (which are intentionally command-line only). Each " +
            "entry notes whether an equivalent MCP tool already exists. Show the user the " +
            "command and let them run it in their terminal.")]
    public static string DiscoverCliCommands()
    {
        var assembly = typeof(Constants).Assembly;

        var commands = CliCommandCatalog.GetCommands(assembly);

        return JsonSerializer.Serialize(commands, JsonOptions);
    }
}
