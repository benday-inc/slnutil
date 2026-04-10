namespace Benday.SolutionUtil.Api.GitHubActions;

public interface IGitHubActionsInfoProvider
{
    Task<GitHubActionInfo?> GetLatestActionInfoAsync(string owner, string actionName);
}
