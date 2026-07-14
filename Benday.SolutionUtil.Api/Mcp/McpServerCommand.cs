using System.Reflection;

using Benday.CommandsFramework;
using Benday.SolutionUtil.Api.Mcp.Tools;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol;

namespace Benday.SolutionUtil.Api.Mcp;

/// <summary>
/// Runs slnutil as a Model Context Protocol (MCP) server over stdio. The
/// process stays alive, speaking JSON-RPC on stdout/stdin, and exposes the
/// read-only inspection and transform tools to an MCP client (Claude Code,
/// Claude Desktop, VS Code, Visual Studio, Cursor).
/// </summary>
/// <remarks>
/// stdout is the JSON-RPC transport, so nothing may be written to it outside
/// the protocol. All logging is routed to stderr.
/// </remarks>
[Command(Name = Constants.CommandArgumentNameMcpServer,
    IsAsync = true,
    Description = "Run slnutil as an MCP (Model Context Protocol) server over stdio. " +
        "Exposes read-only solution, project, and file inspection tools to AI clients. " +
        "This command runs until the client disconnects; it is normally launched by the " +
        "MCP client, not by hand. See 'mcp-config' to register it with a client.")]
public class McpServerCommand : AsynchronousCommand
{
    public McpServerCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        return new ArgumentCollection();
    }

    protected override async Task OnExecute()
    {
        var builder = Host.CreateApplicationBuilder();

        // stdout is reserved for the JSON-RPC transport. Route all logging to
        // stderr so nothing pollutes the protocol stream.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(consoleOptions =>
        {
            consoleOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        var version = typeof(McpServerCommand).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(McpServerCommand).Assembly.GetName().Version?.ToString()
            ?? "1.0.0";

        builder.Services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = McpServerConstants.ServerName,
                    Version = version
                };

                options.ServerInstructions = McpServerConstants.ServerInstructions;
            })
            .WithStdioServerTransport()
            .WithTools<SolutionInspectionTools>()
            .WithTools<TransformTools>()
            .WithTools<CliDiscoveryTools>()
            .WithTools<LearningResourceTools>();

        using var host = builder.Build();

        await host.RunAsync();
    }
}
