using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            .AsRequired()
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
        var configFilename = Arguments.GetStringValue(Constants.ArgumentNameConfigFilename);
        var configKeyname = Arguments.GetStringValue(Constants.ArgumentNameConnectionStringName);
        var configValue = Arguments.GetStringValue(Constants.ArgumentNameValue);

        Utilities.AssertFileExists(configFilename, Constants.ArgumentNameConfigFilename);

        DatabaseConfigurationUtility.SetConnectionString(
            configFilename, configKeyname, configValue);
    }

    
}
