



namespace Benday.SolutionUtil.Api;

public class ProjectInfo
{
	public string ShortName { get; set; } = string.Empty;
	public string ProjectType { get; set; } = string.Empty;
	public string FolderName { get; set; } = string.Empty;
	public string ProjectName { get; set; } = string.Empty;

	public List<string> PackageReferences { get; set; } = new();
    public List<ProjectDefaultFile> DefaultFiles { get; set; } = new();
    public Dictionary<string, string> ProjectProperties { get; set; } = new();
    public FileInfo? Path { get; set; }
    public SolutionInfo? ParentSolution { get; set; }
    public bool IsPrimaryProject { get; set; }

    public string ProjectNameAsToolName
    {
        get
        {
            return ProjectName.Replace(".", string.Empty);
        }
    }

    public void AddDefaultFile(string fileNameInProject, string templateContents)
    {
        DefaultFiles.Add(new ProjectDefaultFile()
        {
            FileName = fileNameInProject,
            TemplateContents = templateContents
        });
    }

    public void AddPackageReference(string packageName)
    {
        PackageReferences.Add(packageName);
    }

    public void WriteDefaultFiles()
    {
        if (DefaultFiles.Count == 0)
        {
            return;
        }

        if (Path == null)
        {
            throw new InvalidOperationException("Path is null.");
        }

        var projectDirectory = Path.Directory ?? throw new InvalidOperationException("Path.Directory is null.");

        if (ParentSolution == null)
        {
            throw new InvalidOperationException("ParentSolution is null.");
        }

        var apiProject = ParentSolution.Projects.FirstOrDefault(x => x.ShortName == "api");

        if (apiProject == null)
        {
            throw new InvalidOperationException("Could not find api in solution.");
        }

        foreach (var fileToWrite in DefaultFiles)
        {
            var fullFilePath = System.IO.Path.Combine(projectDirectory.FullName, fileToWrite.FileName);

            var contents = 
                fileToWrite.TemplateContents;
                
            contents = contents.Replace(
                    "%%PROJECT_NAMESPACE%%", 
                    GetProjectNamespace());

            contents = 
                contents.Replace(
                    "%%API_PROJECT_NAMESPACE%%", 
                    apiProject.ProjectName);

            File.WriteAllText(fullFilePath, contents);
        }
    }

    public void AddProjectProperty(string propertyName, string propertyValue)
    {
        if (ProjectProperties.ContainsKey(propertyName) == false)
        {
            ProjectProperties.Add(propertyName, propertyValue);
        }
        else
        {
            ProjectProperties[propertyName] = propertyValue;
        }
    }


    private string GetProjectNamespace()
    {
        var returnValue = $"{ProjectName}";

        return returnValue;
    }

}

