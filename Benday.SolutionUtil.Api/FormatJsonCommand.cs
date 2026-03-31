
using System.Text.Json;

using Benday.CommandsFramework;
using Benday.Common;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameFormatJson,
    IsAsync = false,
    Description = "Formats JSON files")]
public class FormatJsonCommand : SynchronousCommand
{
    public FormatJsonCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameFilename)
            .AsNotRequired()
            .FromPositionalArgument(1)
            .WithDescription("Path to file or wildcard to files")
            .WithDefaultValue("*.json");

        args.AddBoolean(Constants.ArgumentNameRecursive)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription("Apply to matching files recursively");

        args.AddBoolean(Constants.ArgumentNameWriteToFile)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription("Write changes back to the file");

        return args;
    }

    protected override void OnExecute()
    {
        WriteLine("Starting...");

        var filepath = Arguments.GetStringValue(Constants.ArgumentNameFilename);

        WriteLine($"Filepath value: {filepath}");

        var containsWildcard = filepath.Contains("*");

        WriteLine($"Contains wildcard: {containsWildcard}");


        var recursive = Arguments.GetBooleanValue(Constants.ArgumentNameRecursive);

        var writeToFile = Arguments.GetBooleanValue(Constants.ArgumentNameWriteToFile);

        WriteLine($"Formatting '{filepath}'...");
        WriteLine($"Recursive: {recursive}");
        WriteLine($"Write to file: {writeToFile}");



        if (containsWildcard == true)
        {
            var isPathRooted = Path.IsPathRooted(filepath);

            string directoryPath;

            if (isPathRooted == false)
            {
                directoryPath = Environment.CurrentDirectory;
            }
            else
            {
                directoryPath = Path.GetDirectoryName(filepath) ??
                    throw new KnownException($"Could not get directory path from '{filepath}'.");
            }

            var files = Directory.GetFiles(
                directoryPath, filepath,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                throw new KnownException($"No files found matching '{filepath}'.");
            }

            foreach (var file in files)
            {
                FormatFile(file, writeToFile);
            }
        }
        else
        {
            var target = new TargetFileInfo(filepath);

            FormatFile(filepath, writeToFile);
        }
    }

    private void FormatFile(string file, bool writeToFile)
    {
        WriteLine($"Formatting '{file}'...");

        var json = File.ReadAllText(file);

        var jsonDocument = JsonDocument.Parse(json);

        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);

        jsonDocument.WriteTo(writer);
        writer.Flush();

        var result = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        if (writeToFile == false)
        {
            WriteLine(result);
        }
        else
        {
            File.WriteAllText(file, result);
        }
    }
}
