using System.Text;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameToBase64String,
    Description = "Encodes a string value as a base 64 string.")]
public class ToBase64Command : SynchronousCommand
{

    public ToBase64Command(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameValue).AsRequired()
            .WithDescription("Value to encode as base64");

        return args;
    }

    protected override void OnExecute()
    {
        var token = Arguments[Constants.ArgumentNameValue].Value;

        var asBase64String = GetTokenAsBase64String(token);

        WriteLine(asBase64String);
    }

    public static string GetTokenAsBase64String(string token)
    {
        return Convert.ToBase64String(
            ASCIIEncoding.ASCII.GetBytes(":" + token));
    }
}
