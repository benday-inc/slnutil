namespace Benday.SolutionUtil.Api;

public class TargetFileInfo
{
    public TargetFileInfo(string originalValue)
    {
        OriginalValue = originalValue;

        HasWildcard = OriginalValue.Contains("*");

        if (HasWildcard == true)
        {
            var indexOfStar = OriginalValue.IndexOf("*");

            var beforeStar = 
                OriginalValue.Substring(0, indexOfStar);

            var afterStar = 
                OriginalValue.Substring(indexOfStar);

            // FullPath = Path.GetDirectoryName(originalValue);

            DirectoryPath = beforeStar;
            FileName = afterStar;
            IsPathRooted = Path.IsPathRooted(DirectoryPath);
            DirectoryPath = Path.GetFullPath(DirectoryPath);
        }
        else
        {
            FileName = Path.GetFileName(OriginalValue);
            DirectoryPath = Path.GetDirectoryName(OriginalValue) ?? string.Empty;
            IsPathRooted = Path.IsPathRooted(DirectoryPath);
            DirectoryPath = Path.GetFullPath(DirectoryPath);
        }
        
    }

    public bool IsPathRooted { get; set; }
    public string OriginalValue { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool HasWildcard { get; set; }
    public string DirectoryPath { get; set; } = string.Empty;
}