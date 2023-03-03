using Benday.CommandsFramework;
namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameSetConnectionString,
    Description = "Set database connection string in appsettings.json.")]
public class SetConnectionStringCommand : SynchronousCommand
{
    public SetConnectionStringCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
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

        args.AddString(Constants.ArgumentNameValue)
            .AsRequired()
            .WithDescription("Connection string value");

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

        var configKeyname = Arguments.GetStringValue(Constants.ArgumentNameConnectionStringName);
        var configValue = Arguments.GetStringValue(Constants.ArgumentNameValue);

        var editor = new JsonEditor(configFilename);

        editor.SetValue(
            configValue, "ConnectionStrings", configKeyname);
    }


}
