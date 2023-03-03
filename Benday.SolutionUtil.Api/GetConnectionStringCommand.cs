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
            .AsRequired()
            .WithDescription("Path to json config file");

        args.AddString(Constants.ArgumentNameConnectionStringName)
            .AsRequired()
            .WithDescription("Name of the connection string to set");

        return args;
    }

    protected override void OnExecute()
    {
        var configFilename = Arguments.GetStringValue(Constants.ArgumentNameConfigFilename);
        var configKeyname = Arguments.GetStringValue(Constants.ArgumentNameConnectionStringName);

        Utilities.AssertFileExists(configFilename, Constants.ArgumentNameConfigFilename);

        var value = DatabaseConfigurationUtility.GetConnectionString(
                configFilename, configKeyname);

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
