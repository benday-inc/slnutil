using System.Diagnostics;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api.Mcp;

/// <summary>
/// Prints ready-to-paste MCP client configuration for slnutil, and can register
/// or unregister the server at user scope for clients that support command-line
/// installation (Claude Code, VS Code).
/// </summary>
[Command(Name = Constants.CommandArgumentNameMcpConfig,
    IsAsync = false,
    Description = "Print MCP client configuration for the slnutil server (Claude Code, " +
        "Claude Desktop, VS Code, Visual Studio, Cursor), or install/uninstall it at user " +
        "scope. Run with no arguments to print config for every supported client.")]
public class McpConfigCommand : SynchronousCommand
{
    public McpConfigCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameClientType)
            .AsNotRequired()
            .WithDescription(
                "Target client: claudecode, claudedesktop, vscode, visualstudio, or cursor. " +
                "Omit to print configuration for all clients.");

        args.AddBoolean(Constants.ArgumentNameInstall)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription(
                "Register the server at user scope (Claude Code / VS Code only) instead of " +
                "just printing config.");

        args.AddBoolean(Constants.ArgumentNameUninstall)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription("Remove the user-scope registration (Claude Code only).");

        return args;
    }

    protected override void OnExecute()
    {
        var install = Arguments.GetBooleanValue(Constants.ArgumentNameInstall);
        var uninstall = Arguments.GetBooleanValue(Constants.ArgumentNameUninstall);

        McpClientType? clientType = null;

        if (Arguments.HasValue(Constants.ArgumentNameClientType))
        {
            clientType = ParseClientType(Arguments.GetStringValue(Constants.ArgumentNameClientType));
        }

        if (install || uninstall)
        {
            if (clientType is null)
            {
                throw new KnownException(
                    $"Specify a client with /{Constants.ArgumentNameClientType}: when using " +
                    $"/{Constants.ArgumentNameInstall} or /{Constants.ArgumentNameUninstall}.");
            }

            if (uninstall)
            {
                RunClientCli(McpClientSetup.BuildUninstallCliArgs(clientType.Value), "uninstall", clientType.Value);
            }
            else
            {
                RunClientCli(McpClientSetup.BuildInstallCliArgs(clientType.Value), "install", clientType.Value);
            }

            return;
        }

        var clients = clientType is null
            ? Enum.GetValues<McpClientType>()
            : new[] { clientType.Value };

        foreach (var client in clients)
        {
            PrintConfigFor(client);
        }
    }

    private void PrintConfigFor(McpClientType client)
    {
        WriteLine("======================================================================");
        WriteLine(McpClientSetup.GetClientDisplayName(client));
        WriteLine("======================================================================");
        WriteLine(McpClientSetup.GetConfigLocationHint(client));
        WriteLine(string.Empty);
        WriteLine(McpClientSetup.BuildConfigJson(client));
        WriteLine(string.Empty);

        var installArgs = McpClientSetup.BuildInstallCliArgs(client);

        if (installArgs is not null)
        {
            WriteLine("Or install at user scope with:");
            WriteLine("  " + FormatCommandLine(installArgs));
            WriteLine(string.Empty);
        }
    }

    private void RunClientCli(IReadOnlyList<string>? cliArgs, string operation, McpClientType client)
    {
        if (cliArgs is null || cliArgs.Count == 0)
        {
            throw new KnownException(
                $"{McpClientSetup.GetClientDisplayName(client)} does not support command-line " +
                $"{operation}. Use the printed configuration instead (run 'slnutil " +
                $"{Constants.CommandArgumentNameMcpConfig} /{Constants.ArgumentNameClientType}:" +
                $"{client}').");
        }

        var fileName = cliArgs[0];
        var arguments = cliArgs.Skip(1).ToList();

        WriteLine($"Running: {FormatCommandLine(cliArgs)}");
        WriteLine(string.Empty);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            foreach (var arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo)
                ?? throw new KnownException($"Could not start '{fileName}'.");

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(stdout) == false)
            {
                WriteLine(stdout.TrimEnd());
            }

            if (string.IsNullOrWhiteSpace(stderr) == false)
            {
                WriteLine(stderr.TrimEnd());
            }

            if (process.ExitCode == 0)
            {
                WriteLine($"{operation} succeeded.");
            }
            else
            {
                throw new KnownException(
                    $"'{fileName}' exited with code {process.ExitCode}.");
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new KnownException(
                $"Could not find '{fileName}' on the PATH. Install it or use the printed " +
                $"configuration instead.");
        }
    }

    private static string FormatCommandLine(IReadOnlyList<string> cliArgs)
    {
        return string.Join(" ", cliArgs.Select(arg =>
            arg.Contains(' ') || arg.Contains('{') ? $"'{arg}'" : arg));
    }

    private static McpClientType ParseClientType(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty);

        return normalized switch
        {
            "claudecode" or "claude" => McpClientType.ClaudeCode,
            "claudedesktop" or "desktop" => McpClientType.ClaudeDesktop,
            "vscode" or "code" or "visualstudiocode" => McpClientType.VisualStudioCode,
            "visualstudio" or "vs" or "vs2022" or "vs2026" => McpClientType.VisualStudio,
            "cursor" => McpClientType.Cursor,
            _ => throw new KnownException(
                $"Unknown client '{value}'. Valid values: claudecode, claudedesktop, " +
                "vscode, visualstudio, cursor.")
        };
    }
}
