using System;

namespace Benday.SolutionUtil.Api.GitHubActions;

public class GitHubActionInfo
{
    public GitHubActionInfo(string input)
    {
    }


    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}
