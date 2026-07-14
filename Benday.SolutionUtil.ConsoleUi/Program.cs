using System.Diagnostics;
using System.Reflection;
using System.Text;

using Benday.CommandsFramework;
using Benday.SolutionUtil.Api;

class Program
{
    static void Main(string[] args)
    {
        var assembly = typeof(StringUtility).Assembly;

        var versionInfo =
            FileVersionInfo.GetVersionInfo(
                Assembly.GetExecutingAssembly().Location);

        var options = new DefaultProgramOptions();

        options.Version = $"v{versionInfo.FileVersion}";
        options.ApplicationName = "Solution & Project Utilities";
        options.Website = "https://www.benday.com";
        options.UsesConfiguration = false;

        var program = new DefaultProgram(options, assembly);

        program.Run(args);

        WriteAttributionFooter(args);
    }

    /// <summary>
    /// Prints a one-line attribution footer after every command runs. Skipped
    /// for the MCP server, whose stdout is the JSON-RPC transport and must not
    /// contain any non-protocol text.
    /// </summary>
    private static void WriteAttributionFooter(string[] args)
    {
        var isMcpServer = args.Length > 0 &&
            string.Equals(args[0], Constants.CommandArgumentNameMcpServer, StringComparison.OrdinalIgnoreCase);

        if (isMcpServer == true)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Benjamin Day Consulting, Inc. — benday.com — info@benday.com");
    }
}