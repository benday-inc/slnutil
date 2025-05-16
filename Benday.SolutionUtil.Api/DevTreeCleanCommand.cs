using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameDevTreeClean,
    Description = "Clean development folder tree. Removes node_modules, .git, bin, obj, and TestResults folders.")]
public class DevTreeCleanCommand : SynchronousCommand
{

    public DevTreeCleanCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
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

        args.AddBoolean(Constants.ArgumentNameKeepGitRepo)
            .AsNotRequired()
            .WithDescription("If true, skips delete of .git folders and preserves any git repositories. Default value is true. Set this value to false to delete .git folders.")
            .WithDefaultValue(true);

        args.AddBoolean(Constants.ArgumentNameKeepNodeModules)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("If true, skips delete of node_modules folders. Default value is false.")
            .WithDefaultValue(false);

        return args;
    }

    protected override void OnExecute()
    {
        var rootDir = Environment.CurrentDirectory;

        if (Arguments.HasValue(Constants.ArgumentNameRootDirectory) == true)
        {
            rootDir = Arguments.GetStringValue(Constants.ArgumentNameRootDirectory);
        }

        if (Directory.Exists(rootDir) == false)
        {
            throw new KnownException($"Starting directory '{rootDir}' does not exist.");
        }

        var keepNodeModules = Arguments.GetBooleanValue(Constants.ArgumentNameKeepNodeModules);

        bool keepGit = false;

        if (Arguments.GetBooleanValue(Constants.ArgumentNameKeepGitRepo) == true)
        {
            WriteLine("Skipping deletion of .git folders.");
            keepGit = true;
        }

        CleanDirectory(rootDir, keepGit, keepNodeModules);
    }

    public void CleanDirectory(string fromDir, bool keepGit, bool keepNodeModules)
    {
        DirectoryInfo dirInfo = new DirectoryInfo(fromDir);

        var allDirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);

        var pathSeparator = Path.DirectorySeparatorChar;

        foreach (var dir in allDirs)
        {
            string dirFullNameToLower = dir.FullName.ToLower();

            if (keepNodeModules == true &&
                dirFullNameToLower.EndsWith($"{pathSeparator}node_modules") == true ||
                dirFullNameToLower.Contains($"{pathSeparator}node_modules") == true)
            {
                continue;
            }

            if (keepGit == false &&
                dirFullNameToLower.EndsWith($"{pathSeparator}.git") == true ||
                dirFullNameToLower.EndsWith($"{pathSeparator}bin") == true ||
                dir.FullName.EndsWith($"{pathSeparator}obj") == true ||
                dir.FullName.EndsWith($"{pathSeparator}node_modules") == true ||
                dir.FullName.EndsWith($"{pathSeparator}packages") == true ||
                dir.FullName.EndsWith($"{pathSeparator}TestResults") == true)
            {
                WriteLine($"Deleting directory '{dir.FullName}'");

                try
                {
                    dir.Delete(true);
                    WriteLine("...DONE");
                }
                catch (Exception ex)
                {
                    WriteLine($"Error: {Environment.NewLine}{ex.Message}");
                }
            }
            else if (keepGit == true &&
                dirFullNameToLower.EndsWith($"{pathSeparator}bin") == true ||
                dir.FullName.EndsWith($"{pathSeparator}obj") == true ||
                dir.FullName.EndsWith($"{pathSeparator}node_modules") == true ||
                dir.FullName.EndsWith($"{pathSeparator}packages") == true ||
                dir.FullName.EndsWith($"{pathSeparator}TestResults") == true)
            {
                WriteLine($"Deleting directory '{dir.FullName}'");

                try
                {
                    dir.Delete(true);
                    WriteLine("...DONE");
                }
                catch (Exception ex)
                {
                    WriteLine($"Error: {Environment.NewLine}{ex.Message}");
                }
            }
        }
    }

    public void DeleteFile(System.IO.FileInfo deleteThis)
    {
        WriteLine($"Deleting directory '{deleteThis.FullName}'");
        try
        {
            deleteThis.Delete();
            WriteLine("...DONE");
        }
        catch (Exception ex)
        {
            WriteLine($"...Error: {Environment.NewLine}{ex.Message}");
        }
    }

}
