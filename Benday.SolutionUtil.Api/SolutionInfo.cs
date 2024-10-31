
using System.Diagnostics;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

public class SolutionInfo
{
	public string Name { get; set; } = string.Empty;
	public List<ProjectInfo> Projects { get; set; } = new();
	public List<ProjectReference> ProjectReferences { get; set; } = new();
    public FileInfo? Path { get; set; }

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
}

