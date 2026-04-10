using System;

namespace Benday.SolutionUtil.Api.GitHubActions;

public class GitHubActionVersionInfo
{
    public GitHubActionVersionInfo(GitHubActionInfo current, GitHubActionInfo? latest)
    {
        Current = current ?? throw new ArgumentNullException(nameof(current));
        Latest = latest;
    }

    public GitHubActionInfo Current { get; }
    public GitHubActionInfo? Latest { get; }

    public bool NeedsUpgrade(bool cleanup = false)
    {
        if (Latest == null)
        {
            return false;
        }

        if (!IsSameAction(Current, Latest))
        {
            return false;
        }

        if (Current.VersionType == GitHubActionVersionType.MajorTag &&
            Latest.VersionType == GitHubActionVersionType.MajorTag)
        {
            return GetMajorVersion(Current.Version) < GetMajorVersion(Latest.Version);
        }

        if (cleanup &&
            Current.VersionType == GitHubActionVersionType.SpecificTag &&
            Latest.VersionType == GitHubActionVersionType.MajorTag)
        {
            return GetMajorVersion(Current.Version) < GetMajorVersion(Latest.Version);
        }

        return false;
    }

    private static bool IsSameAction(GitHubActionInfo a, GitHubActionInfo b)
    {
        return string.Equals(a.Owner, b.Owner, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetMajorVersion(string version)
    {
        // strip leading 'v' or 'V', take everything before the first dot
        var trimmed = version.TrimStart('v', 'V');
        var dotIndex = trimmed.IndexOf('.');

        var majorPart = dotIndex >= 0 ? trimmed.Substring(0, dotIndex) : trimmed;

        return int.Parse(majorPart);
    }
}
