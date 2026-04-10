using System;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api.GitHubActions;

public class GitHubActionsParser
{
    private readonly string _yaml;
    private readonly IGitHubActionsInfoProvider? _infoProvider;

    public GitHubActionsParser(string yaml)
    {
        if (string.IsNullOrEmpty(yaml))
        {
            throw new ArgumentException("YAML input cannot be null or empty", nameof(yaml));
        }

        _yaml = yaml;
    }

    public GitHubActionsParser(string yaml, IGitHubActionsInfoProvider infoProvider) : this(yaml)
    {
        if (infoProvider == null)
        {
            throw new ArgumentNullException(nameof(infoProvider));
        }

        _infoProvider = infoProvider;
    }

    /// <summary>
    /// Gets all the GitHub actions used in the YAML file. This is done by looking for lines that start with searchFor and then parsing the action information from those lines. If a line cannot be parsed, it will be skipped. We don't want to fail the entire parsing process just because one line is malformed.
    /// </summary>
    /// <returns></returns>
    public GitHubActionInfo[] GetAllActions()
    {
        var returnValues = new List<GitHubActionInfo>();

        var lines = GetAllLines();
        
        PopulateMatchingLines(returnValues, lines, "uses:");
        PopulateMatchingLines(returnValues, lines, "- uses:");

        var uniqueValues = returnValues.DistinctBy(a => a.ToString());

        return uniqueValues.ToArray();
    }

    private static void PopulateMatchingLines(List<GitHubActionInfo> returnValues, string[] lines, string searchFor)
    {
        var usesLines = lines.Where(l => l.TrimStart().StartsWith(searchFor));

        foreach (var line in usesLines)
        {
            var usesPart = line.TrimStart().Substring(searchFor.Length).Trim();

            try
            {
                var actionInfo = new GitHubActionInfo(usesPart);
                returnValues.Add(actionInfo);
            }
            catch (ArgumentException)
            {
                // if we can't parse it, then just skip it. We don't want to fail the entire parsing process just because one line is malformed.
            }
        }
    }


    private string[] GetAllLines()
    {
        var reader = new StringReader(_yaml);
        var lines = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    public async Task<GitHubActionVersionInfo[]> GetAllActionsWithLatestInfoAsync()
    {
        if (_infoProvider == null)
        {
            throw new InvalidOperationException(
                "Cannot get actions with latest info because no IGitHubActionsInfoProvider was provided.");
        }

        var actions = GetAllActions();
        var result = new List<GitHubActionVersionInfo>();

        foreach (var action in actions)
        {
            var latestInfo = await _infoProvider.GetLatestActionInfoAsync(action.Owner, action.Name);
            result.Add(new GitHubActionVersionInfo(action, latestInfo));
        }

        return result.ToArray();
    }

    public async Task<GitHubActionVersionInfo[]> GetAllActionsThatNeedUpdatesAsync(
        ITextOutputProvider? outputProvider = null, bool cleanup = false)
    {
        if (_infoProvider == null)
        {
            throw new InvalidOperationException(
                "Cannot get actions that need updates because no IGitHubActionsInfoProvider was provided.");
        }

        var actions = GetAllActions();
        var versions = await GetAllActionsWithLatestInfoAsync();

        outputProvider?.WriteLine($"Found {versions.Length} actions with latest info.");

        foreach (var item in versions)
        {
            var currentVersionString = item.Current?.ToString() ?? "null";
            var latestVersionString = item.Latest?.ToString() ?? "null";

            outputProvider?.WriteLine($"Action '{currentVersionString}' has latest version '{latestVersionString}'.");
        }

        var result = versions.Where(v => v.NeedsUpgrade(cleanup)).ToArray();

        return result;
    }

    public async Task<string> UpdateYamlAsync(ITextOutputProvider? outputProvider = null, bool cleanup = false)
    {
        if (_infoProvider == null)
        {
            throw new InvalidOperationException(
                "Cannot update YAML because no IGitHubActionsInfoProvider was provided.");
        }

        var actionsToUpdate = await GetAllActionsThatNeedUpdatesAsync(outputProvider, cleanup);

        outputProvider?.WriteLine($"Found {actionsToUpdate.Length} actions that need updates.");

        var updatedYaml = _yaml;

        foreach (var actionInfo in actionsToUpdate)
        {
            if (actionInfo.Latest == null || actionInfo.Current == null || 
                actionInfo.Current.ToString() == actionInfo.Latest.ToString())
            {
                continue;
            }

            updatedYaml = updatedYaml.Replace(actionInfo.Current.ToString(), actionInfo.Latest.ToStringForTagUpgrade());
            outputProvider?.WriteLine($"Updated '{actionInfo.Current}' to '{actionInfo.Latest}'.");
        }

        return updatedYaml;
    }

}
