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
