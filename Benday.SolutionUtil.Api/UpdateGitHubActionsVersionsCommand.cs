using Benday.CommandsFramework;

using Benday.SolutionUtil.Api.GitHubActions;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameUpdateGitHubActionsVersions,
    IsAsync = true,
    Description = "Reads a GitHub Actions YAML file and updates the action versions to latest.")]
public class UpdateGitHubActionsVersionsCommand : AsynchronousCommand
{
    public UpdateGitHubActionsVersionsCommand(
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
            .WithDescription("Name of the GitHub Actions YAML file to update.");

        return args;
    }

    protected override async Task OnExecute()
    {
        string filename;

        if (Arguments.HasValue(Constants.ArgumentNameFilename))
        {
            filename = Arguments.GetPathToFile(Constants.ArgumentNameFilename);

            if (!File.Exists(filename))
            {
                throw new KnownException($"File '{filename}' does not exist.");
            }
        }
        else
        {
            filename = FindYamlFile();
        }

        WriteLine($"Updating GitHub Actions versions in '{filename}'...");

        var yaml = await File.ReadAllTextAsync(filename);

        using var httpClient = new HttpClient();

        // could you add the required browser strings to the http client so that GitHub doesn't reject the requests?
        // httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("slnutil");

        var infoProvider = new DefaultGitHubActionsInfoProvider(httpClient);
        var parser = new GitHubActionsParser(yaml, infoProvider);

        var updatedYaml = await parser.UpdateYamlAsync(_OutputProvider);

        await File.WriteAllTextAsync(filename, updatedYaml);

        WriteLine("Done.");
    }

    private string FindYamlFile()
    {
        var yamlFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.yaml");
        var ymlFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.yml");

        var allFiles = yamlFiles.Concat(ymlFiles).ToArray();

        if (allFiles.Length == 0)
        {
            throw new KnownException(
                "No YAML files (*.yaml or *.yml) found in the current directory.");
        }

        if (allFiles.Length == 1)
        {
            return allFiles[0];
        }

        var fileNames = string.Join(Environment.NewLine, allFiles.Select(f => Path.GetFileName(f)));

        throw new KnownException(
            $"Multiple YAML files found in the current directory. Please specify a filename:{Environment.NewLine}{fileNames}");
    }
}
