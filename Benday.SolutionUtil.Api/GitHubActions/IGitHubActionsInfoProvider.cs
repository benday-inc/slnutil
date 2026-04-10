namespace Benday.SolutionUtil.Api.GitHubActions;

public interface IGitHubActionsInfoProvider
{
    GitHubActionInfo? GetLatestActionInfo(string owner, string actionName);
}
