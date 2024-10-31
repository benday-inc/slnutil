

namespace Benday.SolutionUtil.Api;

public class ProjectInfo
{
	public string ShortName { get; set; } = string.Empty;
	public string ProjectType { get; set; } = string.Empty;
	public string FolderName { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;

	public List<string> PackageReferences { get; set; } = new();
    public FileInfo? Path { get; set; }


    internal void AddPackageReference(string packageName)
    {
        PackageReferences.Add(packageName);
    }

}

