using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameUpdateBicepVersions,
    IsAsync = true,
    Description = "Reads bicep file or files and updates the api versions to latest.")]
public class UpdateBicepVersionsCommand : AsynchronousCommand
{
    public UpdateBicepVersionsCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();
        args.AddFile(Constants.ArgumentNameFilename)
            .AsNotRequired()
            .FromPositionalArgument(1)
            .WithDescription("Name of the bicep file, if you want to update just one file.");

        args.AddBoolean(Constants.ArgumentNamePreview)
            .AsNotRequired()
            .WithDefaultValue(false)
            .AllowEmptyValue()
            .WithDescription("Do not save changes only preview the changes.");

        return args;
    }

    protected override async Task OnExecute()
    {
        var preview = Arguments.GetBooleanValue(Constants.ArgumentNamePreview);

        string? filename = null;

        if (Arguments.HasValue(Constants.ArgumentNameFilename) == false)
        {
            WriteLine("No filename specified, will update all bicep files in the current directory.");
        }
        else
        {
            filename = Arguments.GetPathToFile(Constants.ArgumentNameFilename);

            // Check if the file exists
            if (File.Exists(filename) == false)
            {
                throw new KnownException($"File '{filename}' does not exist.");
            }
        }

        if (filename == null)
        {
            // get all bicep files in the current directory
            var bicepFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.bicep");

            WriteLine($"Found {bicepFiles.Length} bicep files in the current directory.");

            foreach (var file in bicepFiles)
            {
                await UpdateBicepFile(file, preview);
            }
        }
        else
        {
            await UpdateBicepFile(filename, preview);
        }
    }

    static async Task<string?> GetLatestApiVersion(string providerNamespace, string resourceType)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"provider show --namespace {providerNamespace} --query \"resourceTypes[?resourceType=='{resourceType}'].apiVersions[0]\" -o tsv",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new ProcessRunner(startInfo);

        process.Run();

        if (process.IsError == true)
        {
            throw new InvalidOperationException(
                $"Error running command: {process.ErrorText}");
        }

        return process.OutputText.Trim();
    }


    private async Task UpdateBicepFile(string file, bool preview)
    {
        WriteLine($"Checking bicep file: {file}...");

        var resourceRegex = new Regex(@"resource\s+(\w+)\s+'([^/]+/[^@]+)@([^']+)'");
        var lines = await File.ReadAllLinesAsync(file);

        var needsSave = false;

        foreach (var line in lines)
        {
            var match = resourceRegex.Match(line);
            if (!match.Success) continue;

            var resourceName = match.Groups[1].Value;
            var resourceType = match.Groups[2].Value;
            var currentApiVersion = match.Groups[3].Value;

            var providerNamespace = resourceType.Split('/')[0];
            var resourceTypeName = resourceType.Split('/')[1];

            var latestApiVersion = await GetLatestApiVersion(providerNamespace, resourceTypeName);
            if (latestApiVersion == null)
            {
                WriteLine($"Could not find latest API version for {resourceType}");
                continue;
            }

            if (currentApiVersion != latestApiVersion)
            {
                WriteLine($"🔄 Resource '{resourceName}' ({resourceType})");
                WriteLine($"   Bicep file version: {currentApiVersion}");
                WriteLine($"   Latest version:     {latestApiVersion}");
                WriteLine();

                if (!preview)
                {
                    // Update the line in the file
                    var updatedLine = line.Replace($"'{resourceType}@{currentApiVersion}'", $"'{resourceType}@{latestApiVersion}'");
                    var index = Array.IndexOf(lines, line);
                    lines[index] = updatedLine;
                    needsSave = true;
                }
            }
        }

        if (preview == false && needsSave == true)
        {
            WriteLine($"Saving changes to {file}...");
            await File.WriteAllLinesAsync(file, lines);
            WriteLine("Changes saved.");
        }
        else if (preview == true && needsSave == true)
        {
            WriteLine("Preview mode: changes would be saved, but no changes were made to the file.");
        }
        else
        {
            WriteLine("No updates needed for this file.");
        }
    }

}
