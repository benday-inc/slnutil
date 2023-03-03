using Benday.CommandsFramework;
namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameGetConnectionString,
    Description = "Get database connection string in appsettings.json.")]
public class GetConnectionStringCommand : SynchronousCommand
{
    public GetConnectionStringCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameConfigFilename)
            .AsNotRequired()
            .WithDescription("Path to json config file");

        args.AddString(Constants.ArgumentNameConnectionStringName)
            .AsRequired()
            .WithDescription("Name of the connection string to set");

        return args;
    }

    protected override void OnExecute()
    {
        string? configFilename;

        if (Arguments.HasValue(Constants.ArgumentNameConfigFilename) == true)
        {
            configFilename = Arguments.GetStringValue(Constants.ArgumentNameConfigFilename);
        }
        else
        {
            configFilename =
                ProjectUtilities.FindFirstFileName(
                    Environment.CurrentDirectory, "appsettings.json");
        }

        if (configFilename == null)
        {
            throw new KnownException("Could not find appsettings.json file");
        }
        else
        {
            Utilities.AssertFileExists(configFilename, Constants.ArgumentNameConfigFilename);
        }

        WriteLine($"Using '{configFilename}'...");
        WriteLine(string.Empty);

        var configKeyname = Arguments.GetStringValue(Constants.ArgumentNameConnectionStringName);

        Utilities.AssertFileExists(configFilename, Constants.ArgumentNameConfigFilename);

        var editor = new JsonEditor(configFilename);

        var value = editor.GetValue(
                "ConnectionStrings", configKeyname);

        WriteLine(value);
    }

    protected void AssertFileExists(string path, string argumentName)
    {
        if (File.Exists(path) == false)
        {
            var info = new FileInfo(path);

            string message = String.Format(
                "File for argument '{0}' was not found at '{1}'.",
                argumentName,
                info.FullName);

            throw new FileNotFoundException(
                message, path);
        }
    }
}
