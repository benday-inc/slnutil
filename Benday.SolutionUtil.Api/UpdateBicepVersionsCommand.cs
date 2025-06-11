using System;
using System.Diagnostics;
using System.Text.Json;
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

        args.AddBoolean(Constants.ArgumentNameAllowPreviewVersions)
            .AsNotRequired()
            .WithDefaultValue(false)
            .AllowEmptyValue()
            .WithDescription("Allow preview versions for resources.");

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
        var allowPreviewVersions = Arguments.GetBooleanValue(Constants.ArgumentNameAllowPreviewVersions);

        WriteLine($"Preview mode: {preview}");
        WriteLine($"Allow preview versions: {allowPreviewVersions}");

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
                await UpdateBicepFile(file, preview, allowPreviewVersions);
            }
        }
        else
        {
            await UpdateBicepFile(filename, preview, allowPreviewVersions);
        }
    }

    private Task<string?> GetLatestApiVersion(string providerNamespace, string resourceType, bool allowPreviewVersions)
    {

        string args;


        args = $"provider show --namespace Microsoft.App --query \"resourceTypes[?resourceType=='jobs'].apiVersions\" -o json";

        var startInfo = new ProcessStartInfo
        {
            FileName = "az",
            Arguments = args,
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

        var apiVersions = DeserializeApiVersions(process.OutputText);
        if (apiVersions.Count == 0)
        {
            WriteLine($"No API versions found for {providerNamespace}/{resourceType}");
            return Task.FromResult<string?>(null);
        }
        else
        {
            // Sort the API versions in descending order
            apiVersions.Sort((a, b) => string.Compare(b, a, StringComparison.Ordinal));
            // If allowPreviewVersions is false, filter out preview versions
            if (!allowPreviewVersions)
            {
                apiVersions = apiVersions.Where(v => !v.Contains("preview", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            // Return the latest API version
            var latestApiVersion = apiVersions.FirstOrDefault();
            if (latestApiVersion == null)
            {
                WriteLine($"No valid API versions found for {providerNamespace}/{resourceType}");
                return Task.FromResult<string?>(null);
            }
            else
            {
                WriteLine($"Latest API version for {providerNamespace}/{resourceType} is {latestApiVersion}");
                return Task.FromResult<string?>(latestApiVersion);
            }
        }
    }

    private List<string> DeserializeApiVersions(string json)
    {
        var outer = JsonSerializer.Deserialize<List<List<string>>>(json);
        return outer?.FirstOrDefault() ?? new List<string>();
    }


    private async Task UpdateBicepFile(string file, bool preview, bool allowPreviewVersions)
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

            var latestApiVersion = await GetLatestApiVersion(providerNamespace, resourceTypeName, allowPreviewVersions);
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
