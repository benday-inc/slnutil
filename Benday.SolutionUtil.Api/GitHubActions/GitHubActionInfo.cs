using System;
using System.Text.RegularExpressions;

namespace Benday.SolutionUtil.Api.GitHubActions;

public class GitHubActionInfo
{
    private static readonly Regex MajorTagPattern = new(@"^v\d+$", RegexOptions.IgnoreCase);
    private static readonly Regex SpecificTagPattern = new(@"^v\d+\.\d+(\.\d+)*$", RegexOptions.IgnoreCase);
    private static readonly Regex ShaPattern = new(@"^[0-9a-fA-F]{7,40}$");

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
        VersionType = ResolveVersionType(Version);
    }

    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public GitHubActionVersionType VersionType { get; set; } = GitHubActionVersionType.Unknown;

    private static GitHubActionVersionType ResolveVersionType(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return GitHubActionVersionType.Unknown;
        }

        if (MajorTagPattern.IsMatch(version))
        {
            return GitHubActionVersionType.MajorTag;
        }

        if (SpecificTagPattern.IsMatch(version))
        {
            return GitHubActionVersionType.SpecificTag;
        }

        if (ShaPattern.IsMatch(version))
        {
            return GitHubActionVersionType.Sha;
        }

        return GitHubActionVersionType.Branch;
    }

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

    public string ToStringForTagUpgrade()
    {
        return $"{Owner}/{Name}@v{GetMajorVersion(Version)}";
    }

    private string GetMajorVersion(string version)
    {
        // strip leading 'v' or 'V', take everything before the first dot
        var trimmed = version.TrimStart('v', 'V');
        var dotIndex = trimmed.IndexOf('.');

        var majorPart = dotIndex >= 0 ? trimmed.Substring(0, dotIndex) : trimmed;

        return majorPart;
    }
}
