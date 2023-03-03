using Benday.CommandsFramework;
namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameSetJsonValue,
    Description = "Set a string value in a json file.")]
public class SetJsonValueCommand : SynchronousCommand
{
    public SetJsonValueCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameConfigFilename)
            .AsNotRequired()
            .WithDescription("Path to json config file");

        args.AddString(Constants.ArgumentNameLevel1)
            .AsRequired()
            .WithDescription("First level json property name to set");

        args.AddString(Constants.ArgumentNameLevel2)
            .AsNotRequired()
            .WithDescription("Second level json property name to set");

        args.AddString(Constants.ArgumentNameLevel3)
            .AsNotRequired()
            .WithDescription("Third level json property name to set");

        args.AddString(Constants.ArgumentNameLevel4)
            .AsNotRequired()
            .WithDescription("Fourth level json property name to set");

        args.AddString(Constants.ArgumentNameValue)
            .AsRequired()
            .WithDescription("String value to set");

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

        var newValue = Arguments.GetStringValue(Constants.ArgumentNameValue);
        var level1 = Arguments.GetStringValue(Constants.ArgumentNameLevel1);
        string? level2 = null;
        string? level3 = null;
        string? level4 = null;

        var editor = new JsonEditor(configFilename);

        if (Arguments.HasValue(Constants.ArgumentNameLevel2) == true &&
            Arguments.HasValue(Constants.ArgumentNameLevel3) == true &&
            Arguments.HasValue(Constants.ArgumentNameLevel4) == true)
        {
            level2 = Arguments.GetStringValue(Constants.ArgumentNameLevel2);
            level3 = Arguments.GetStringValue(Constants.ArgumentNameLevel3);
            level4 = Arguments.GetStringValue(Constants.ArgumentNameLevel4);

            editor.SetValue(
                newValue, level1, level2, level3, level4);
        }
        else if (Arguments.HasValue(Constants.ArgumentNameLevel2) == true &&
            Arguments.HasValue(Constants.ArgumentNameLevel3) == true)
        {
            level2 = Arguments.GetStringValue(Constants.ArgumentNameLevel2);
            level3 = Arguments.GetStringValue(Constants.ArgumentNameLevel3);

            editor.SetValue(
                newValue, level1, level2, level3);
        }
        else if (Arguments.HasValue(Constants.ArgumentNameLevel2) == true &&
            Arguments.HasValue(Constants.ArgumentNameLevel3) == false)
        {
            level2 = Arguments.GetStringValue(Constants.ArgumentNameLevel2);

            editor.SetValue(
                newValue,
                level1, level2);
        }
        else
        {
            editor.SetValue(
                newValue,
                level1);
        }
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
