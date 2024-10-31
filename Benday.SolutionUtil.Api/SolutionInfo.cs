
using System.Diagnostics;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

public class SolutionInfo
{
	public string Name { get; set; } = string.Empty;
	public List<ProjectInfo> Projects { get; set; } = new();
	public List<ProjectReference> ProjectReferences { get; set; } = new();
    public FileInfo? Path { get; set; }
	public List<ProjectDefaultFile> DefaultFiles { get; set; } = new();

    public SolutionInfo()
	{
		Projects = new List<ProjectInfo>();
		ProjectReferences = new List<ProjectReference>();
	}

	public ProjectInfo AddProject(string shortName, string projectType, string folderName, string projectName)
	{
		var project = new ProjectInfo
		{
			ShortName = shortName,
			ProjectType = projectType,
			FolderName = folderName,
			ProjectName = projectName
		};

		Projects.Add(project);

		project.ParentSolution = this;
		
		return project;
	}

	public ProjectReference AddProjectReference(string fromProjectShortName, string toProjectShortName)
	{
		var projectReference = new ProjectReference
		{
			FromProjectShortName = fromProjectShortName,
			ToProjectShortName = toProjectShortName
		};

		ProjectReferences.Add(projectReference);
		return projectReference;
	}

    public void AddProjectReference(ProjectInfo fromProject, ProjectInfo toProject)
    {
        AddProjectReference(fromProject.ShortName, toProject.ShortName);
    }

    public void AddDefaultFile(string fileNameInSolution, string templateContents)    
    {
        DefaultFiles.Add(new ProjectDefaultFile()
        {
            FileName = fileNameInSolution,
            TemplateContents = templateContents
        });
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

        var primaryProject = Projects.FirstOrDefault(x => x.IsPrimaryProject == true);

        if (primaryProject == null)
        {
            throw new InvalidOperationException("Could not find the primary project in solution.");
        }

        foreach (var fileToWrite in DefaultFiles)
        {
            var fullFilePath = System.IO.Path.Combine(projectDirectory.FullName, fileToWrite.FileName);

            var contents = 
                fileToWrite.TemplateContents;
                
            contents = 
                contents.Replace(
                    "%%PRIMARY_PROJECT_NAME%%", 
                    primaryProject.ProjectName);

			contents = 
                contents.Replace(
                    "%%PRIMARY_PROJECT_TOOL_NAME%%", 
                    primaryProject.ProjectNameAsToolName);


            File.WriteAllText(fullFilePath, contents);
        }
    }

}

