using Benday.CommandsFramework;
using Benday.Common;
using Microsoft.Data.SqlClient;

namespace Benday.SolutionUtil.Api;

[Command(
    Name = Constants.CommandArgumentNameRunSql,
    Description = "Execute SQL command or SQL script file against a database.")]
public class RunSqlCommand : SynchronousCommand
{
    public RunSqlCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameConnectionString)
            .AsRequired()
            .WithDescription("Connection string to the database");

        args.AddString(Constants.ArgumentNameSqlQuery)
            .AsNotRequired()
            .WithDescription("SQL query to execute");

        args.AddFile(Constants.ArgumentNameSqlFile)
            .AsNotRequired()
            .WithDescription("Path to SQL file to execute");

        return args;
    }

    protected override void OnExecute()
    {
        var connectionString = Arguments.GetStringValue(Constants.ArgumentNameConnectionString);
        
        var hasSqlCommand = Arguments.HasValue(Constants.ArgumentNameSqlQuery);
        var hasSqlFile = Arguments.HasValue(Constants.ArgumentNameSqlFile);

        if (!hasSqlCommand && !hasSqlFile)
        {
            throw new KnownException("You must provide either a SQL command or a SQL file path.");
        }

        if (hasSqlCommand && hasSqlFile)
        {
            throw new KnownException("You cannot provide both a SQL command and a SQL file path. Choose one.");
        }

        string sqlToExecute;

        if (hasSqlCommand)
        {
            sqlToExecute = Arguments.GetStringValue(Constants.ArgumentNameSqlQuery);
            WriteLine("Executing SQL command...");
        }
        else
        {
            var sqlFilePath = Arguments.GetPathToFile(
                Constants.ArgumentNameSqlFile, true);

            WriteLine($"Reading SQL file: {sqlFilePath}");

            sqlToExecute = File.ReadAllText(sqlFilePath);
        }

        ExecuteSql(connectionString, sqlToExecute);
    }

    private void ExecuteSql(
        string connectionString, string sql)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            
            WriteLine("Opening connection...");
            connection.Open();
            WriteLine("Connected to database.");

            // Split by GO statements if they exist
            var sqlBatches = SplitSqlBatches(sql);

            foreach (var batch in sqlBatches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                using var command = new SqlCommand(batch, connection);
                
                WriteLine("Executing batch...");
                
                var affectedRows = command.ExecuteNonQuery();
                
                if (affectedRows >= 0)
                {
                    WriteLine($"Affected rows: {affectedRows}");
                }
                else
                {
                    WriteLine("Command executed successfully.");
                }
            }

            WriteLine("SQL execution completed successfully.");
        }
        catch (SqlException ex)
        {
            WriteLine("SQL execution failed.");
            WriteLine(string.Empty);
            WriteLine($"Error: {ex.Message}");
            throw new KnownException($"SQL execution failed: {ex.Message}");
        }
    }

    private string[] SplitSqlBatches(string sql)
    {
        // Split by GO statements (case insensitive, on its own line)
        var batches = System.Text.RegularExpressions.Regex.Split(
            sql, 
            @"^\s*GO\s*$", 
            System.Text.RegularExpressions.RegexOptions.Multiline | 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return batches;
    }
}