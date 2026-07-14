using System.Reflection;

using Benday.CommandsFramework;

using ModelContextProtocol;

namespace Benday.SolutionUtil.Api.Mcp;

/// <summary>
/// Runs an existing slnutil <see cref="CommandBase"/> in-process with its text
/// output captured to a string, so it can be exposed as an MCP tool without
/// duplicating the command's logic. The command's report never reaches stdout
/// (which is the JSON-RPC transport); it is returned to the caller instead.
/// </summary>
public static class CommandToolRunner
{
    /// <summary>
    /// Construct <typeparamref name="TCommand"/> with the supplied arguments,
    /// execute it, and return everything it wrote to its output provider.
    /// </summary>
    /// <typeparam name="TCommand">A slnutil command type.</typeparam>
    /// <param name="arguments">
    /// Argument name/value pairs, keyed by the same names the command declares
    /// in <c>GetArguments()</c> (see <see cref="Constants"/>). Entries with a
    /// null value are skipped so the command's own defaults apply.
    /// </param>
    /// <exception cref="McpException">
    /// Thrown when the command raises a <see cref="KnownException"/> (a friendly,
    /// user-facing error) so the message reaches the MCP client cleanly.
    /// </exception>
    public static string Run<TCommand>(IDictionary<string, string?> arguments)
        where TCommand : CommandBase
    {
        var commandAttribute = typeof(TCommand).GetCustomAttribute<CommandAttribute>()
            ?? throw new InvalidOperationException(
                $"Type '{typeof(TCommand).FullName}' is missing a [Command] attribute.");

        var capturedOutput = new StringBuilderTextOutputProvider();

        var options = new DefaultProgramOptions
        {
            ApplicationName = McpServerConstants.ServerName,
            UsesConfiguration = false,
            OutputProvider = capturedOutput
        };

        var executionInfo = new CommandExecutionInfo
        {
            CommandName = commandAttribute.Name,
            Options = options,
            Arguments = new Dictionary<string, string>()
        };

        foreach (var argument in arguments)
        {
            if (argument.Value is not null)
            {
                executionInfo.Arguments[argument.Key] = argument.Value;
            }
        }

        var command = (CommandBase)Activator.CreateInstance(
            typeof(TCommand), executionInfo, capturedOutput)!;

        try
        {
            switch (command)
            {
                case SynchronousCommand synchronousCommand:
                    synchronousCommand.Execute();
                    break;
                case AsynchronousCommand asynchronousCommand:
                    asynchronousCommand.ExecuteAsync().GetAwaiter().GetResult();
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Command type '{typeof(TCommand).FullName}' is neither " +
                        "synchronous nor asynchronous.");
            }
        }
        catch (KnownException ex)
        {
            throw new McpException(ex.Message);
        }

        var output = capturedOutput.GetOutput();

        return string.IsNullOrWhiteSpace(output)
            ? "(command completed with no output)"
            : output;
    }
}
