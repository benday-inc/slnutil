using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameCreate,
    Description = "Create a solution")]
internal class CreateSolutionCommand : SynchronousCommand
{
    public const string PackageName_FluentAssertions = "FluentAssertions";
    public const string SourceDirNameInSolution = "src";
    public const string TestDirNameInSolution = "test";

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
        returnValue.Add("commands", "Console Application with Benday.CommandsFramework");
        returnValue.Add("maui", ".NET Maui Application with xUnit tests");

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
        else if (solutionType == "commands")
        {
            CreateConsoleSolution(solution, rootNamespace);
            AddCommands(solution);
        }
        else if (solutionType == "maui")
        {
            CreateMauiSolution(solution, rootNamespace);
        }
        else
        {
            throw new KnownException($"Unknown solution type '{solutionType}'.");
        }

        var rootDirInfo = new DirectoryInfo(rootDir);

        Create(solution, rootDirInfo);
    }

    private void AddCommands(SolutionInfo solution)
    {
        foreach (var project in solution.Projects)
        {
            project.PackageReferences.Add("Benday.CommandsFramework");
        }

        var apiProject = solution.Projects.FirstOrDefault(x => x.ShortName == "api");
        var consoleProject = solution.Projects.FirstOrDefault(x => x.ShortName == "console");

        if (apiProject == null)
        {
            throw new InvalidOperationException("Could not find api project.");
        }

        if (consoleProject == null)
        {
            throw new InvalidOperationException("Could not find console project.");
        }

        AddDefaultFile(solution, "install.ps1", "commands-install-ps1");
        AddDefaultFile(solution, "uninstall.ps1", "commands-uninstall-ps1");
        AddDefaultFile(consoleProject, "Program.cs", "commands-program-cs");
        AddDefaultFile(apiProject, "SampleCommand.cs", "commands-sample-command-cs");
        AddDefaultFile(apiProject, "SampleAsyncCommand.cs", "commands-sample-async-command-cs");

        consoleProject.AddProjectProperty("PackAsTool", "True");
        consoleProject.AddProjectProperty("GeneratePackageOnBuild", "True");
        consoleProject.AddProjectProperty("Authors", "your-name-here");
        consoleProject.AddProjectProperty("Copyright", DateTime.Now.Year.ToString());
        consoleProject.AddProjectProperty("AssemblyVersion", "1.0.0");
        consoleProject.AddProjectProperty("Version", "1.0.0");
        consoleProject.AddProjectProperty("Description", "Your description here.");
        consoleProject.AddProjectProperty("PackageReleaseNotes", "");
        consoleProject.AddProjectProperty("AssemblyName", consoleProject.ProjectNameAsToolName);
    }

    private void AddDefaultFile(ProjectInfo project,
        string fileNameInProject,
        string templateName)
    {
        var templateContents = GetTemplateFile(templateName);

        project.AddDefaultFile(fileNameInProject, templateContents);
    }

    private void AddDefaultFile(SolutionInfo solution,
        string fileNameInSolution,
        string templateName)
    {
        var templateContents = GetTemplateFile(templateName);

        solution.AddDefaultFile(fileNameInSolution, templateContents);
    }

    private string GetTemplateFile(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var assemblyLocation = assembly.Location ?? throw new InvalidOperationException("Could not get assembly location.");

        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? throw new InvalidOperationException("Could not get assembly directory.");

        var templatesDir = Path.Combine(assemblyDir, "templates");

        var resourcePath = Path.Combine(templatesDir, $"{templateName}.txt");

        if (File.Exists(resourcePath) == false)
        {
            throw new InvalidOperationException($"Could not find template '{templateName}'.");
        }

        return File.ReadAllText(resourcePath);
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

        WriteProjectProperties(solution);

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

        WriteDefaultFiles(solution);        

        WriteLine("Writing default files for solution...");
        solution.WriteDefaultFiles();

        WriteLine("Done.");
    }

    private void WriteProjectProperties(SolutionInfo solution)
    {
        foreach (var project in solution.Projects)
        {
            if (project.ProjectProperties.Count > 0)
            {
                WriteLine($"Adding project properties to project '{project.ProjectName}'...");

                foreach (var key in project.ProjectProperties.Keys)
                {
                    var value = project.ProjectProperties[key];

                    WriteLine($"Adding property to project '{project.ProjectName}' - {key} = {value}");

                    if (project.Path == null)
                    {
                        throw new InvalidOperationException($"Path for project '{project.ProjectName}' is null.");
                    }

                    var projectFileContents = File.ReadAllText(project.Path.FullName);

                    var doc = XDocument.Parse(projectFileContents);

                    var root = doc.Root ?? throw new InvalidOperationException("Root element is null.");

                    var result =
                        ProjectUtilities.SetProjectPropertyElement(
                            project.Path.FullName, root, key, value);

                    if (result == null || result.ValueChanged == true)
                    {
                        var settings = new XmlWriterSettings
                        {
                            OmitXmlDeclaration = true,
                            Indent = true
                        };

                        using var writer = XmlWriter.Create(project.Path.FullName, settings);

                        doc.Save(writer);

                        writer.Close();
                    }
                }
            }
        }
    }


    private void WriteDefaultFiles(SolutionInfo solution)
    {
        if (solution.Path == null)
        {
            throw new InvalidOperationException("Solution path is null.");
        }

        foreach (var project in solution.Projects)
        {
            if (project.DefaultFiles.Count > 0)
            {
                WriteLine($"Adding default files to project '{project.ProjectName}'...");

                project.WriteDefaultFiles();
            }
        }
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
        var webProject = solution.AddProject("web", "mvc", SourceDirNameInSolution, $"{rootNamespace}.WebUi");
        webProject.IsPrimaryProject = true;

        var apiProject = solution.AddProject("api", "classlib", SourceDirNameInSolution, $"{rootNamespace}.Api");
        var unitTestProject = solution.AddProject("unittests", "xunit", TestDirNameInSolution, $"{rootNamespace}.UnitTests");
        var integrationTestsProject = solution.AddProject("integrationtests", "xunit", TestDirNameInSolution, $"{rootNamespace}.IntegrationTests");

        solution.AddProjectReference(unitTestProject, apiProject);
        solution.AddProjectReference(integrationTestsProject, apiProject);
        solution.AddProjectReference(integrationTestsProject, webProject);
        solution.AddProjectReference(webProject, apiProject);

        unitTestProject.AddPackageReference(PackageName_FluentAssertions);
        integrationTestsProject.AddPackageReference(PackageName_FluentAssertions);
    }

    private void CreateWebApiSolution(SolutionInfo solution, string rootNamespace)
    {
        var webProject = solution.AddProject("webapi", "webapi", SourceDirNameInSolution, $"{rootNamespace}.WebApi");
        webProject.IsPrimaryProject = true;

        var apiProject = solution.AddProject("api", "classlib", SourceDirNameInSolution, $"{rootNamespace}.Api");
        var unitTestProject = solution.AddProject("unittests", "xunit", TestDirNameInSolution, $"{rootNamespace}.UnitTests");
        var integrationTestsProject = solution.AddProject("integrationtests", "xunit", TestDirNameInSolution, $"{rootNamespace}.IntegrationTests");

        solution.AddProjectReference(unitTestProject, apiProject);
        solution.AddProjectReference(integrationTestsProject, apiProject);
        solution.AddProjectReference(integrationTestsProject, webProject);
        solution.AddProjectReference(webProject, apiProject);

        unitTestProject.AddPackageReference(PackageName_FluentAssertions);
        integrationTestsProject.AddPackageReference(PackageName_FluentAssertions);
    }


    private void CreateConsoleSolution(SolutionInfo solution, string rootNamespace)
    {
        var consoleProject = solution.AddProject("console", "console", SourceDirNameInSolution, $"{rootNamespace}.ConsoleUi");
        consoleProject.IsPrimaryProject = true;

        var apiProject = solution.AddProject("api", "classlib", SourceDirNameInSolution, $"{rootNamespace}.Api");
        var unitTestProject = solution.AddProject("unittests", "xunit", TestDirNameInSolution, $"{rootNamespace}.UnitTests");

        solution.AddProjectReference(unitTestProject, apiProject);
        solution.AddProjectReference(consoleProject, apiProject);

        unitTestProject.AddPackageReference(PackageName_FluentAssertions);
    }

    private void CreateMauiSolution(SolutionInfo solution, string rootNamespace)
    {
        var mauiProject = solution.AddProject("maui", "maui", SourceDirNameInSolution, $"{rootNamespace}.Ui");
        mauiProject.IsPrimaryProject = true;

        mauiProject.AddDefaultFile("MauiProgram.cs", GetTemplateFile("maui-mauiprogram-cs"));

        var apiProject = solution.AddProject("api", "mauilib", SourceDirNameInSolution, $"{rootNamespace}.Api");
        var unitTestProject = solution.AddProject("unittests", "xunit", TestDirNameInSolution, $"{rootNamespace}.UnitTests");

        solution.AddProjectReference(unitTestProject, apiProject);
        solution.AddProjectReference(mauiProject, apiProject);

        unitTestProject.AddPackageReference(PackageName_FluentAssertions);

        apiProject.AddProjectProperty("TargetFrameworks", "net8;net8.0-android;net8.0-ios;net8.0-maccatalyst");

        mauiProject.AddPackageReference("CommunityToolkit.Maui");
        apiProject.AddPackageReference("CommunityToolkit.Maui");
    }

}
