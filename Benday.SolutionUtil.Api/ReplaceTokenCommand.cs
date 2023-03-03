using System.Linq;

using Benday.CommandsFramework;
namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameReplaceToken,
    Description = "Replace token in file.")]
public class ReplaceTokenCommand : SynchronousCommand
{
    public ReplaceTokenCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameConfigFilename)
            .AsRequired()
            .WithDescription("Path to file");

        args.AddString(Constants.ArgumentNameToken)
            .AsRequired()
            .WithDescription("Token to replace");

        args.AddString(Constants.ArgumentNameValue)
            .AsRequired()
            .WithDescription("String value to set");

        return args;
    }

    protected override void OnExecute()
    {
        var configFilename = Arguments.GetStringValue(Constants.ArgumentNameConfigFilename);
        Utilities.AssertFileExists(configFilename, Constants.ArgumentNameConfigFilename);

        var configToken = Arguments.GetStringValue(Constants.ArgumentNameToken);
        var configValue = Arguments.GetStringValue(Constants.ArgumentNameValue);

        var text = File.ReadAllText(configFilename);

        if (text.Contains(configToken) == true)
        {
            text = text.Replace(configToken, configValue);

            File.WriteAllText(configFilename, text);
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
