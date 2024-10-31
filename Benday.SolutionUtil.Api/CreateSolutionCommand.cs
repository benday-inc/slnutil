using System.Diagnostics;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameCreate,
    Description = "Create a solution")]
internal class CreateSolutionCommand : SynchronousCommand
{
    public const string PackageName_FluentAssertions = "FluentAssertions";

    public CreateSolutionCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameRootDirectory)
            .AsNotRequired()
            .WithDescription(
                "Starting directory. If not supplied, the tool uses the current directory.");

        args.AddString(Constants.CommandArgumentNameSolutionType)
            .AsRequired()
            .WithDescription(
                $"Type of solution to create. Valid values are: {GetValidSolutionTypeStrings()}")
            .FromPositionalArgument(1);

        args.AddString(Constants.CommandArgumentNameNamespace)
            .AsRequired()
            .WithDescription(
                "This is the root namespace for the solution.  For example: Benday.SampleApp")
            .FromPositionalArgument(2);

        return args;
    }

    protected override void DisplayUsage()
    {
        base.DisplayUsage();

        WriteLine();

        var validTypes = GetValidSolutionTypes();

        WriteLine("Valid solution types:");

        var longestKey = validTypes.Keys.Max(x => x.Length);

        foreach (var key in validTypes.Keys)
        {
            WriteLine($"  {key.PadRight(longestKey)} - {validTypes[key]}");
        }
    }

    private string GetValidSolutionTypeStrings()
    {
        var validTypes = GetValidSolutionTypes();

        var returnValue = string.Join(", ", validTypes.Keys);

        return returnValue;
    }


    private Dictionary<string, string> GetValidSolutionTypes()
    {
        var returnValue = new Dictionary<string, string>();

        returnValue.Add("webapi", "ASP.NET Web API");
        returnValue.Add("mvc", "ASP.NET MVC");
        returnValue.Add("console", "Console Application");

        return returnValue;
    }


    protected override void OnExecute()
    {
        var rootDir = Environment.CurrentDirectory;

        if (Arguments.HasValue(Constants.ArgumentNameRootDirectory) == true)
        {
            rootDir = Arguments.GetPathToDirectory(Constants.ArgumentNameRootDirectory, true);
        }

        var solutionTypeValue = Arguments.GetStringValue(Constants.CommandArgumentNameSolutionType);
        var rootNamespace = Arguments.GetStringValue(Constants.CommandArgumentNameNamespace);

        var solutionTypes = GetValidSolutionTypes();

        if (solutionTypes.ContainsKey(solutionTypeValue) == false)
        {
            throw new KnownException($"Invalid solution type '{solutionTypeValue}'.  Valid values are: {GetValidSolutionTypeStrings()}");
        }

        var solutionTypeDescription = solutionTypes[solutionTypeValue];

        WriteLine($"Creating solution of type '{solutionTypeValue}' ({solutionTypeDescription}) in directory '{rootDir}' with root namespace '{rootNamespace}'.");

        Create(solutionTypeValue, rootDir, rootNamespace);
    }

    private void Create(string solutionType, string rootDir, string rootNamespace)
    {
        if (Directory.Exists(rootDir) == false)
        {
            throw new KnownException($"Starting directory '{rootDir}' does not exist.");
        }

        var solution = new SolutionInfo();

        solution.Name = Path.GetFileName(rootNamespace);

        if (solutionType == "webapi")
        {
            CreateWebApiSolution(solution, rootNamespace);
        }
        else if (solutionType == "mvc")
        {
            CreateMvcSolution(solution, rootNamespace);
        }
        else if (solutionType == "console")
        {
            CreateConsoleSolution(solution, rootNamespace);
        }
        else
        {
            throw new KnownException($"Unknown solution type '{solutionType}'.");
        }

        var rootDirInfo = new DirectoryInfo(rootDir);

        Create(solution, rootDirInfo);
    }

    private void Create(SolutionInfo solution, DirectoryInfo rootDirInfo)
    {
        var solutionDir = Path.Combine(rootDirInfo.FullName, solution.Name);

        if (Directory.Exists(solutionDir) == true)
        {
            throw new KnownException($"Directory '{solutionDir}' already exists.");
        }

        var solutionDirInfo = Directory.CreateDirectory(solutionDir);

        var solutionPath = CreateSolution(solution, solutionDirInfo);


        WriteLine($"Solution created at '{solutionPath.FullName}'.");

        WriteLine("Creating projects...");

        foreach (var project in solution.Projects)
        {
            var projectDir = Path.Combine(solutionDir, project.FolderName);

            var projectDirInfo = Directory.CreateDirectory(projectDir);

            CreateProject(project, projectDirInfo);
        }

        WriteLine("Creating project references...");

        foreach (var projectReference in solution.ProjectReferences)
        {
            if (projectReference is null)
            {
                throw new InvalidOperationException("Project reference is null.");
            }


            CreateProjectReference(solution, projectReference);
        }

        WriteLine("Adding projects to solution...");

        AddProjectsToSolution(solution, solutionDirInfo);
        AddPackageReferences(solution, solutionDirInfo);

        WriteLine("Done.");
    }

    private void AddPackageReferences(SolutionInfo solution, DirectoryInfo solutionDirInfo)
    {
        foreach (var project in solution.Projects)
        {
            if (project.PackageReferences.Count > 0)
            {
                AddPackageReferences(project);
            }            
        }
    }

    private void AddPackageReferences(ProjectInfo project)
    {
        foreach (var packageName in project.PackageReferences)
        {
            WriteLine($"Adding package reference '{packageName}' to project '{project.ProjectName}'.");

            if (project.Path == null)
            {
                throw new InvalidOperationException($"Path for project '{project.ProjectName}' is null.");
            }

            var startInfo = new ProcessStartInfo("dotnet", $"add {project.Path.FullName} package {packageName}")
            {
                WorkingDirectory = project.Path.DirectoryName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            RunProcess(startInfo);

            WriteLine($"Added package reference '{packageName}' to project '{project.ProjectName}'.");
        }
    }

    private void AddProjectsToSolution(SolutionInfo solution, DirectoryInfo solutionDirInfo)
    {
        if (solution.Path == null)
        {
            throw new InvalidOperationException("Solution path is null.");
        }

        foreach (var project in solution.Projects)
        {
            if (project.Path == null)
            {
                throw new InvalidOperationException($"Project path is null for project '{project.ProjectName}'.");
            }

            WriteLine($"Adding project '{project.ProjectName}' to solution.");

            var startInfo = new ProcessStartInfo("dotnet", $"sln {solution.Path.FullName} add {project.Path.FullName}")
            {
                WorkingDirectory = solutionDirInfo.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            RunProcess(startInfo);

            WriteLine($"Added project '{project.ProjectName}' to solution.");
        }
    }


    private void CreateProjectReference(SolutionInfo solution, ProjectReference projectReference)
    {
        var fromProject = solution.Projects.FirstOrDefault(x => x.ShortName == projectReference.FromProjectShortName);

        if (fromProject == null)
        {
            throw new InvalidOperationException($"Could not find project '{projectReference.FromProjectShortName}'.");
        }

        var toProject = solution.Projects.FirstOrDefault(x => x.ShortName == projectReference.ToProjectShortName);

        if (toProject == null)
        {
            throw new InvalidOperationException($"Could not find project '{projectReference.ToProjectShortName}'.");
        }

        var fromProjectPath = fromProject.Path ?? throw new InvalidOperationException($"Path for project '{fromProject.ShortName}' is null.");
        var toProjectPath = toProject.Path ?? throw new InvalidOperationException($"Path for project '{toProject.ShortName}' is null.");

        WriteLine($"Adding reference from '{fromProjectPath.Name}' to '{toProjectPath.Name}'.");

        var startInfo = new ProcessStartInfo("dotnet", $"add {fromProjectPath.FullName} reference {toProjectPath.FullName}")
        {
            WorkingDirectory = fromProjectPath.DirectoryName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        RunProcess(startInfo);

        WriteLine($"Added reference from '{fromProjectPath.Name}' to '{toProjectPath.Name}'.");
    }

    private void CreateProject(ProjectInfo project, DirectoryInfo projectDirInfo)
    {
        WriteLine($"Creating project '{project.ProjectName}' in '{projectDirInfo.FullName}'.");

        var projectFilePath = Path.Combine(
            projectDirInfo.FullName, 
            project.ProjectName, 
            $"{project.ProjectName}.csproj");

        var startInfo = new ProcessStartInfo("dotnet", $"new {project.ProjectType} -n {project.ProjectName}")
        {
            WorkingDirectory = projectDirInfo.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        RunProcess(startInfo);

        var projectFile = new FileInfo(projectFilePath);

        if (projectFile.Exists == false)
        {
            throw new InvalidOperationException($"Could not find project file '{projectFile.FullName}' after create.");
        }

        project.Path = projectFile;
    }


    private FileInfo CreateSolution(SolutionInfo solution, DirectoryInfo solutionDir)
    {
        var solutionPath = Path.Combine(solutionDir.FullName, $"{solution.Name}.sln");

        var startInfo = new ProcessStartInfo("dotnet", $"new sln -n {solution.Name}")
        {
            WorkingDirectory = solutionDir.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        RunProcess(startInfo);

        solution.Path = new FileInfo(solutionPath);

        return solution.Path;
    }

    private void RunProcess(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start process.");

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            // read error output
            var errorOutput = process.StandardError.ReadToEnd();

            WriteLine(errorOutput);

            throw new KnownException($"Error creating solution.  Exit code was {process.ExitCode}.");
        }
    }

    private void CreateMvcSolution(SolutionInfo solution, string rootNamespace)
    {
        var webProject = solution.AddProject("web", "mvc", "src", $"{rootNamespace}.WebUi");
        var apiProject = solution.AddProject("api", "classlib", "src", $"{rootNamespace}.Api");
        var unitTestProject = solution.AddProject("unittests", "xunit", "src", $"{rootNamespace}.UnitTests");
        var integrationTestsProject = solution.AddProject("integrationtests", "xunit", "src", $"{rootNamespace}.IntegrationTests");

        solution.AddProjectReference(unitTestProject, apiProject);
        solution.AddProjectReference(integrationTestsProject, apiProject);
        solution.AddProjectReference(integrationTestsProject, webProject);
        solution.AddProjectReference(webProject, apiProject);

        unitTestProject.AddPackageReference(PackageName_FluentAssertions);
        integrationTestsProject.AddPackageReference(PackageName_FluentAssertions);
    }

    private void CreateWebApiSolution(SolutionInfo solution, string rootNamespace)
    {
        var webProject = solution.AddProject("webapi", "webapi", "src", $"{rootNamespace}.WebApi");
        var apiProject = solution.AddProject("api", "classlib", "src", $"{rootNamespace}.Api");
        var unitTestProject = solution.AddProject("unittests", "xunit", "src", $"{rootNamespace}.UnitTests");
        var integrationTestsProject = solution.AddProject("integrationtests", "xunit", "src", $"{rootNamespace}.IntegrationTests");

        solution.AddProjectReference(unitTestProject, apiProject);
        solution.AddProjectReference(integrationTestsProject, apiProject);
        solution.AddProjectReference(integrationTestsProject, webProject);
        solution.AddProjectReference(webProject, apiProject);

        unitTestProject.AddPackageReference(PackageName_FluentAssertions);
        integrationTestsProject.AddPackageReference(PackageName_FluentAssertions);
    }


    private void CreateConsoleSolution(SolutionInfo solution, string rootNamespace)
    {
        var consoleProject = solution.AddProject("console", "console", "src", $"{rootNamespace}.ConsoleUi");
        var apiProject = solution.AddProject("api", "classlib", "src", $"{rootNamespace}.Api");
        var unitTestProject = solution.AddProject("unittests", "xunit", "src", $"{rootNamespace}.UnitTests");

        solution.AddProjectReference(unitTestProject, apiProject);
        solution.AddProjectReference(consoleProject, apiProject);

        unitTestProject.AddPackageReference(PackageName_FluentAssertions);
    }

}
