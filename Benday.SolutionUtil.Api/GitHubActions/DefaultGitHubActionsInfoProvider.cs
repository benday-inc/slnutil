namespace Benday.SolutionUtil.Api.GitHubActions;

public class DefaultGitHubActionsInfoProvider : IGitHubActionsInfoProvider
{
    public GitHubActionInfo? GetLatestActionInfo(string owner, string actionName)
    {
        // for now, we will just return null. We will implement this later when we have a better understanding of how to get this information from GitHub.
        return null;
    }
}
