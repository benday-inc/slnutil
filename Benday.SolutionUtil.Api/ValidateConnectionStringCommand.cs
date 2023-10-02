using Benday.CommandsFramework;
using Benday.Common;
using Benday.JsonUtilities;

using Microsoft.Data.SqlClient;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameValidateConnectionString,
    Description = "Validate that specified connection string can connect to SQL Server.")]
public class ValidateConnectionStringCommand : SynchronousCommand
{
    public ValidateConnectionStringCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
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
            .WithDescription("Name of the connection string to validate");

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

        if (value == null)
        {
            throw new KnownException($"Could not find connection string for '{configKeyname}'");
        }

        WriteLine(value.SafeToString());

        ValidateConnection(value);
    }

    private void ValidateConnection(string value)
    {
        try
        {
            using var connection = new SqlConnection(value);

            WriteLine("Opening connection...");

            connection.Open();

            WriteLine("Connected to database.");

            using var command = new SqlCommand("SELECT @@VERSION", connection);

            var result = command.ExecuteScalar();

            if (result is string)
            {
                var resultAsString = result.ToString();

                WriteLine($"Version: {resultAsString}");
            }
            else
            {
                WriteLine($"Could not retrieve version.");
            }
        }
        catch(SqlException ex)
        {
            WriteLine("Connection failed.");
            WriteLine(string.Empty);
            WriteLine($"Error: {ex.Message}");
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
