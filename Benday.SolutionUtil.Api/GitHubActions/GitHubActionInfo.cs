using System;

namespace Benday.SolutionUtil.Api.GitHubActions;

public class GitHubActionInfo
{
    public GitHubActionInfo()
    {
    }

    public GitHubActionInfo(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input string cannot be null or empty", nameof(input));
        }

        var parts = input.Split('@');

        if (parts.Length != 2)
        {
            throw new ArgumentException("Input string must be in the format 'owner/name@version'", nameof(input));
        }

        var nameParts = parts[0].Split('/');

        if (nameParts.Length != 2)
        {
            throw new ArgumentException("Input string must be in the format 'owner/name@version'", nameof(input));
        }

        Owner = nameParts[0];
        Name = nameParts[1];
        Version = parts[1];
    }

    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    override public string ToString()
    {
        return $"{Owner}/{Name}@{Version}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GitHubActionInfo other)
            return false;

        return string.Equals(Owner, other.Owner, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Owner?.ToLowerInvariant(),
            Name?.ToLowerInvariant(),
            Version?.ToLowerInvariant()
        );
    }
}
