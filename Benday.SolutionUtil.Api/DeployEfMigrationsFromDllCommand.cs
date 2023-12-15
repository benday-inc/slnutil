using System.Diagnostics;
using System.Reflection;
using System.Text;

using Benday.CommandsFramework;

using Microsoft.EntityFrameworkCore;

namespace Benday.SolutionUtil.Api;
[Command(Name = Constants.CommandArgumentNameDeployEfMigrations, Description = "Deploy EF Core Migrations from DLL binaries.")]
public class DeployEfMigrationsFromDllCommand : SynchronousCommand
{

    public DeployEfMigrationsFromDllCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameBinariesDirectory)
            .AsNotRequired()
            .WithDefaultValue(Environment.CurrentDirectory)
            .WithDescription("Path to EF Core migration binaries. Defaults to current directory.");

        //args.AddBoolean(Constants.ArgumentNameListProjects)
        //    .AsNotRequired()
        //    .AllowEmptyValue()
        //    .WithDescription("List projects in solutions");

        //args.AddBoolean(Constants.ArgumentNameCommaSeparatedValues)
        //    .AsNotRequired()
        //    .AllowEmptyValue()
        //    .WithDescription("Output results as comma-separated values");

        args.AddBoolean(Constants.ArgumentNameVerbose)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription("Output results as comma-separated values");

        return args;
    }


    protected override void OnExecute()
    {
        var verbose = Arguments.GetBooleanValue(Constants.ArgumentNameVerbose);

        if (verbose == true)
        {
            WriteLine("Verbose output enabled.");
        }

        var binariesDir = 
            Arguments.GetStringValue(
                Constants.ArgumentNameBinariesDirectory);

        // assert that the directory exists
        ProjectUtilities.AssertDirectoryExists(binariesDir, Constants.ArgumentNameBinariesDirectory);

        var rootDir = Environment.CurrentDirectory;
        
        var processPath = Environment.ProcessPath;
        
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;

        if (verbose == true)
        {
            WriteLine($"Root directory: {rootDir}");
            WriteLine($"Process path: {processPath}");
            WriteLine($"Assembly location: {assemblyLocation}");
        }

        var assemblyDir = new FileInfo(assemblyLocation).Directory ?? 
            throw new InvalidOperationException($"Cannot find directory for assembly location: '{assemblyLocation}'");

        WriteLine($"Assembly directory: {assemblyDir.FullName}");

        var pathToEfDll = Path.Combine(assemblyDir.FullName, "ef.dll");
        var pathToEfRuntimeConfigJson = Path.Combine(assemblyDir.FullName, "ef.runtimeconfig.json");

        // assert that the files exist
        ProjectUtilities.AssertFileExists(pathToEfDll, "ef.dll");
        ProjectUtilities.AssertFileExists(pathToEfRuntimeConfigJson, "ef.runtimeconfig.json");

        if (verbose == true)
        {
            WriteLine($"Path to ef.dll: {pathToEfDll}");
            WriteLine($"Path to ef.runtimeconfig.json: {pathToEfRuntimeConfigJson}");
        }

        var binariesDirInfo = new DirectoryInfo(binariesDir);
        FileInfo depsJsonPath;
        FileInfo runtimeConfigJsonPath;

        if (binariesDirInfo.GetFiles("*.deps.json").Count() == 0)
        {
            WriteLine($"No *.deps.json files found in '{binariesDir}'.");
            throw new KnownException($"No *.deps.json files found in '{binariesDir}'. Please specify a startup DLL using the /{Constants.ArgumentNameStartupDll} argument.");
        }
        else if (binariesDirInfo.GetFiles("*.deps.json").Count() > 1)
        {
            WriteLine($"More than one *.deps.json files found in '{binariesDir}'.");

            foreach (var item in binariesDirInfo.GetFiles("*.deps.json"))
            {
                WriteLine($"Found deps.json file: {item.FullName}");
            }

            throw new KnownException($"More than one *.deps.json files found in '{binariesDir}'. Please specify a startup DLL using the /{Constants.ArgumentNameStartupDll} argument.");
        }
        else
        {
            depsJsonPath = binariesDirInfo.GetFiles("*.deps.json").FirstOrDefault() ?? throw new InvalidOperationException("No deps.json file found");
        }

        if (binariesDirInfo.GetFiles("*.runtimeconfig.json").Count() == 0)
        {
            WriteLine($"No *.runtimeconfig.json files found in '{binariesDir}'.");
            throw new KnownException($"No *.runtimeconfig.json files found in '{binariesDir}'. Please specify a startup DLL using the /{Constants.ArgumentNameStartupDll} argument.");
        }
        else if (binariesDirInfo.GetFiles("*.runtimeconfig.json").Count() > 1)
        {
            WriteLine($"More than one *.runtimeconfig.json files found in '{binariesDir}'.");
            throw new KnownException($"More than one *.runtimeconfig.json files found in '{binariesDir}'. Please specify a startup DLL using the /{Constants.ArgumentNameStartupDll} argument.");
        }
        else
        {
            runtimeConfigJsonPath = binariesDirInfo.GetFiles("*.runtimeconfig.json").FirstOrDefault() ?? throw new InvalidOperationException("No runtimeconfig.json file found");
        }

        var proposedStartupDll = depsJsonPath.FullName.Replace(".deps.json", ".dll");

        if (File.Exists(proposedStartupDll) == false)
        {
            throw new KnownException($"Could not find startup DLL '{proposedStartupDll}'. Please specify a startup DLL using the /{Constants.ArgumentNameStartupDll} argument.");
        }

        var dbContextType = FindDbContext(binariesDir, depsJsonPath) ?? throw new KnownException("Could not find a DbContext class");
  
        WriteLine($"Found DbContext type: {dbContextType.DbContextType?.FullName}");

        DeployMigrations(binariesDir, pathToEfDll, pathToEfRuntimeConfigJson, proposedStartupDll, dbContextType, depsJsonPath, runtimeConfigJsonPath);
    }

    private void DeployMigrations(string binariesDir, 
        string pathToEfDll, string pathToEfRuntimeConfigJson, 
        string proposedStartupDll, DbContextInfo dbContextType,
        FileInfo depsJsonPath, FileInfo runtimeConfigJsonPath)
    {
        var processInfo = new ProcessStartInfo("dotnet");

        processInfo.ArgumentList.Add("exec");
        processInfo.ArgumentList.Add("--depsfile");
        processInfo.ArgumentList.Add(depsJsonPath.FullName);

        processInfo.ArgumentList.Add("--runtimeconfig");
        processInfo.ArgumentList.Add(runtimeConfigJsonPath.FullName);

        processInfo.ArgumentList.Add(pathToEfDll);

        processInfo.ArgumentList.Add("database");
        processInfo.ArgumentList.Add("update");

        processInfo.ArgumentList.Add("--assembly");
        processInfo.ArgumentList.Add(dbContextType.AssemblyFileName);

        processInfo.ArgumentList.Add("--project-dir");
        processInfo.ArgumentList.Add(binariesDir);

        processInfo.ArgumentList.Add("--data-dir");
        processInfo.ArgumentList.Add(binariesDir);

        processInfo.ArgumentList.Add("--context");
        processInfo.ArgumentList.Add(dbContextType.DbContextType!.Name);

        processInfo.ArgumentList.Add("--verbose");

        processInfo.ArgumentList.Add("--root-namespace");
        processInfo.ArgumentList.Add(dbContextType.DbContextType!.Namespace!);

        processInfo.WorkingDirectory = binariesDir;

        var process = Process.Start(processInfo);

        process?.WaitForExit();
    }

    private DbContextInfo? FindDbContext(Assembly searchInThisAssembly, string assemblyFilePath)
    {
        var dbContextTypes = searchInThisAssembly.GetLoadableTypes().Where(x => x != null && x.IsSubclassOf(typeof(DbContext))).ToList();

        if (dbContextTypes.Count == 0)
        {
            return null;
        }
        else if (dbContextTypes.Count > 1)
        {
            throw new KnownException($"Found more than one DbContext type. Please specify a startup DLL using the /{Constants.ArgumentNameMigrationsDll} argument and /{Constants.ArgumentNameDbContextName} argument.");
        }
        else
        {
            var returnValue = new DbContextInfo()
            {
                Assembly = searchInThisAssembly,
                DbContextType = dbContextTypes[0],
                AssemblyFileName = assemblyFilePath
            };

            return returnValue;
        }
    }

    private DbContextInfo? FindDbContext(string binariesDir, FileInfo depsJsonPath)
    {
        var proposedStartupDll = depsJsonPath.FullName.Replace(".deps.json", ".dll");

        if (File.Exists(proposedStartupDll) == false)
        {
            throw new KnownException($"Could not find startup DLL '{proposedStartupDll}'. Please specify a startup DLL using the /{Constants.ArgumentNameStartupDll} argument.");
        }

        var startupAssemblyFileInfo = new FileInfo(proposedStartupDll);

        var startupAssembly = Assembly.LoadFrom(proposedStartupDll);

        var dbContextType = FindDbContext(startupAssembly, startupAssemblyFileInfo.Name);

        if (dbContextType == null)
        {
            var dllsWithSimilarNames = FindDllsWithSimilarNames(binariesDir, startupAssemblyFileInfo);

            if (dllsWithSimilarNames.Count == 0)
            {
                throw new KnownException($"Could not find DbContext type. Please specify a startup DLL using the /{Constants.ArgumentNameMigrationsDll} argument and /{Constants.ArgumentNameDbContextName} argument.");
            }

            foreach (var proposedDll in dllsWithSimilarNames)
            {
                var assemblyFilePath = Path.Combine(binariesDir, proposedDll);

                var proposedAssembly = Assembly.LoadFrom(assemblyFilePath);

                dbContextType = FindDbContext(proposedAssembly, proposedDll);

                if (dbContextType == null)
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
        }

        return dbContextType;
    }

    private List<string> FindDllsWithSimilarNames(string binariesDir, FileInfo originalFile)
    {
        var originalFileName = originalFile.Name;

        var potentiallySimilarFiles = Directory.GetFiles(binariesDir, $"{originalFileName[0]}*.dll");

        var values = new List<KeyValuePair<int, string>>();

        foreach (var item in potentiallySimilarFiles)
        {
            var fileInfo = new FileInfo(item);
            var fileName = fileInfo.Name;

            if (originalFileName == fileName)
            {
                // same file...skip it
                continue;
            }

            var numberOfMatchingCharacters = Utilities.FindNumberOfMatchingCharacters(originalFileName, fileName);

            if (numberOfMatchingCharacters > 0)
            {
                values.Add(new KeyValuePair<int, string>(numberOfMatchingCharacters, fileName));
            }
        }

        return values.OrderByDescending(x => x.Key).Select(x => x.Value).ToList();
    }
}
