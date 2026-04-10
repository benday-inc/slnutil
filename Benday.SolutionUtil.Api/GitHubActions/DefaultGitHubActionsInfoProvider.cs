using System.Text.Json;

using Benday.Common.Json;

namespace Benday.SolutionUtil.Api.GitHubActions;

public class DefaultGitHubActionsInfoProvider : IGitHubActionsInfoProvider
{
    private const string GitHubApiGetLatestUrl = "https://api.github.com/repos/{0}/{1}/releases/latest";
    private const string GitHubApiGetInfo = "https://api.github.com/repos/{0}/{1}";

    private readonly HttpClient _httpClient;

    public DefaultGitHubActionsInfoProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<GitHubActionInfo?> GetLatestActionInfoAsync(string owner, string actionName)
    {
        var getLatestUrl = string.Format(GitHubApiGetLatestUrl, owner, actionName);
        var getInfoUrl = string.Format(GitHubApiGetInfo, owner, actionName);

        var getInfoContent = await _httpClient.GetStringAsync(getInfoUrl);
        var getLatestContent = await _httpClient.GetStringAsync(getLatestUrl);
        
        var getLatestJson = JsonDocument.Parse(getLatestContent);
        var tagName = getLatestJson.RootElement.SafeGetString("tag_name");

        var getInfoJson = JsonDocument.Parse(getInfoContent);
        var fullName = getInfoJson.RootElement.SafeGetString("full_name");

        return new GitHubActionInfo($"{fullName}@{tagName}");
    }
}
