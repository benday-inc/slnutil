using System.Reflection;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api.Mcp.Tools;

/// <summary>
/// A single slnutil command-line command described for discovery: its name,
/// description, arguments, an example invocation, and whether the same
/// capability is already available as a dedicated MCP tool.
/// </summary>
public sealed class CliCommandDescriptor
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<CliArgumentDescriptor> Arguments { get; init; }
    public required string Example { get; init; }

    /// <summary>
    /// The name of the equivalent MCP tool, or null when this command is only
    /// available from the command line.
    /// </summary>
    public string? McpToolName { get; init; }
}

public sealed class CliArgumentDescriptor
{
    public required string Name { get; init; }
    public required bool IsRequired { get; init; }
    public required string DataType { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Builds the catalog of slnutil command-line commands by reflecting over the
/// assembly's <c>[Command]</c> types. Used by the <c>discover_cli_commands</c>
/// MCP tool so an AI client can point the user at the right terminal command
/// for capabilities that are not (or should not be) exposed as MCP tools.
/// </summary>
public static class CliCommandCatalog
{
    /// <summary>
    /// Maps a CLI command name to the MCP tool that already exposes it. Keep this
    /// in sync as tools are added so <c>discover_cli_commands</c> can tell the
    /// model "there's already a tool for that."
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> CommandToMcpTool =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Constants.CommandArgumentNameListSolutionProjects] = "list_solution_projects",
            [Constants.CommandArgumentNameFindSolutions] = "find_solutions",
            [Constants.CommandArgumentNameListPackagesLegacy] = "list_packages_config",
            [Constants.CommandArgumentNameAssemblyInfo] = "get_assembly_info",
            [Constants.CommandArgumentNameGetConnectionString] = "get_connection_string",
            [Constants.CommandArgumentNameValidateConnectionString] = "validate_connection_string",
            [Constants.CommandArgumentNameToBase64String] = "base64_encode",
            [Constants.CommandArgumentNameFormatJson] = "format_json",
            [Constants.CommandArgumentNameFormatXml] = "format_xml",
            ["printfile"] = "print_file",
            [Constants.CommandArgumentNameClassesFromJson] = "classes_from_json",
        };

    public static IReadOnlyList<CliCommandDescriptor> GetCommands(Assembly assembly)
    {
        var options = new DefaultProgramOptions
        {
            ApplicationName = McpServerConstants.ServerName,
            UsesConfiguration = false
        };

        var utility = new CommandAttributeUtility(options);

        var usages = utility.GetAllCommandUsages(assembly);

        var commands = new List<CliCommandDescriptor>();

        foreach (var usage in usages.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var arguments = new List<CliArgumentDescriptor>();

            foreach (var argument in usage.Arguments)
            {
                arguments.Add(new CliArgumentDescriptor
                {
                    Name = argument.Name,
                    IsRequired = argument.IsRequired,
                    DataType = argument.DataType.ToString(),
                    Description = argument.Description ?? string.Empty
                });
            }

            CommandToMcpTool.TryGetValue(usage.Name, out var mcpToolName);

            commands.Add(new CliCommandDescriptor
            {
                Name = usage.Name,
                Description = usage.Description ?? string.Empty,
                Arguments = arguments,
                Example = BuildExample(usage.Name, arguments),
                McpToolName = mcpToolName
            });
        }

        return commands;
    }

    private static string BuildExample(string commandName, IReadOnlyList<CliArgumentDescriptor> arguments)
    {
        var example = new System.Text.StringBuilder();

        example.Append(McpServerConstants.ServerName);
        example.Append(' ');
        example.Append(commandName);

        foreach (var argument in arguments.Where(a => a.IsRequired))
        {
            example.Append(" /");
            example.Append(argument.Name);
            example.Append(":<");
            example.Append(argument.Name);
            example.Append('>');
        }

        return example.ToString();
    }
}
