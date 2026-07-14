using System.ComponentModel;

using ModelContextProtocol.Server;

namespace Benday.SolutionUtil.Api.Mcp.Tools;

/// <summary>
/// Read-only MCP tools that inspect .NET solutions, projects, packages,
/// assemblies, and connection-string configuration. Each tool wraps an
/// existing slnutil command and returns its report as text.
/// </summary>
[McpServerToolType]
public class SolutionInspectionTools
{
    [McpServerTool(Name = "list_solution_projects", ReadOnly = true),
        Description(
            "List the projects in a .NET solution along with each project's target " +
            "framework. Use this to see what projects a solution contains. Requires the " +
            "absolute path to a .sln or .slnx file.")]
    public static string ListSolutionProjects(
        [Description("Absolute path to the .sln or .slnx solution file to inspect.")]
        string solutionPath)
    {
        return CommandToolRunner.Run<ListSolutionProjectsCommand>(new Dictionary<string, string?>
        {
            [Constants.ArgumentNameSolutionPath] = solutionPath
        });
    }

    [McpServerTool(Name = "find_solutions", ReadOnly = true),
        Description(
            "Find all solution files (.sln and .slnx) beneath a directory. Optionally " +
            "lists the projects in each solution with reference and target-framework " +
            "analysis. Requires the absolute path to the directory to search.")]
    public static string FindSolutions(
        [Description("Absolute path to the directory to search recursively.")]
        string rootDirectory,
        [Description("When true, also list the projects in each solution with reference analysis.")]
        bool listProjects = false)
    {
        var arguments = new Dictionary<string, string?>
        {
            [Constants.ArgumentNameRootDirectory] = rootDirectory
        };

        if (listProjects)
        {
            arguments[Constants.ArgumentNameListProjects] = "true";
        }

        return CommandToolRunner.Run<FindSolutionsCommand>(arguments);
    }

    [McpServerTool(Name = "list_packages_config", ReadOnly = true),
        Description(
            "List NuGet packages referenced via legacy packages.config files beneath a " +
            "directory. Use this to find old-style package references that predate " +
            "PackageReference. Requires the absolute path to the directory to search.")]
    public static string ListPackagesConfig(
        [Description("Absolute path to the directory to search recursively for packages.config files.")]
        string rootDirectory)
    {
        return CommandToolRunner.Run<ListPackagesLegacyStyleCommand>(new Dictionary<string, string?>
        {
            [Constants.ArgumentNameRootDirectory] = rootDirectory
        });
    }

    [McpServerTool(Name = "get_assembly_info", ReadOnly = true),
        Description(
            "Show assembly metadata for a compiled .NET DLL: file dates, size, full " +
            "assembly name, and assembly-level attributes (target framework, version, " +
            "company, etc.). Requires the absolute path to a .dll file.")]
    public static string GetAssemblyInfo(
        [Description("Absolute path to the .dll assembly file to inspect.")]
        string assemblyPath)
    {
        return CommandToolRunner.Run<AssemblyInfoCommand>(new Dictionary<string, string?>
        {
            [Constants.ArgumentNameFilename] = assemblyPath
        });
    }

    [McpServerTool(Name = "get_connection_string", ReadOnly = true),
        Description(
            "Read a named connection string from an appsettings.json (or similar) config " +
            "file. Returns the connection string value. Requires the absolute path to the " +
            "config file and the name of the connection string.")]
    public static string GetConnectionString(
        [Description("Absolute path to the JSON config file (e.g. appsettings.json).")]
        string configFilePath,
        [Description("Name of the connection string under the ConnectionStrings section.")]
        string connectionStringName)
    {
        return CommandToolRunner.Run<GetConnectionStringCommand>(new Dictionary<string, string?>
        {
            [Constants.ArgumentNameConfigFilename] = configFilePath,
            [Constants.ArgumentNameConnectionStringName] = connectionStringName
        });
    }

    [McpServerTool(Name = "validate_connection_string", ReadOnly = true, OpenWorld = true),
        Description(
            "Validate that a named connection string in a config file can actually connect " +
            "to SQL Server. This OPENS A REAL DATABASE CONNECTION and runs SELECT @@VERSION. " +
            "It does not modify any data. Requires the absolute path to the config file and " +
            "the connection string name.")]
    public static string ValidateConnectionString(
        [Description("Absolute path to the JSON config file (e.g. appsettings.json).")]
        string configFilePath,
        [Description("Name of the connection string under the ConnectionStrings section.")]
        string connectionStringName)
    {
        return CommandToolRunner.Run<ValidateConnectionStringCommand>(new Dictionary<string, string?>
        {
            [Constants.ArgumentNameConfigFilename] = configFilePath,
            [Constants.ArgumentNameConnectionStringName] = connectionStringName
        });
    }
}
