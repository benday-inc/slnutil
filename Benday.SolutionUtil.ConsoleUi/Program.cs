using System.Diagnostics;
using System.Reflection;
using System.Text;

using Benday.CommandsFramework;
using Benday.CommandsFramework.Tui;

class Program
{
    static async Task<int> Main(string[] args)
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
        options.StrictArgumentValidation = true;
        options.TuiHost = new SpectreTuiHost();

        var program = new DefaultProgram(options, assembly);

        return await program.RunAsync(args);
    }
}
