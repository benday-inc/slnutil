using System;

namespace Benday.SolutionUtil.Api.GitHubActions;

public class GitHubActionsParser
{
    private readonly string _yaml;

    public GitHubActionsParser(string yaml)
    {
        if (string.IsNullOrEmpty(yaml))
        {
            throw new ArgumentException("YAML input cannot be null or empty", nameof(yaml));
        }

        _yaml = yaml;
    }

    public GitHubActionInfo[] GetAllActions()
    {
        throw new NotImplementedException();
    }

}
