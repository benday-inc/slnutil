using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameDevTreeClean, 
    Description = "Clean development folder tree. Removes node_modules, .git, bin, obj, and TestResults folders.")]
internal class DevTreeCleanCommand : SynchronousCommand
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

        return args;
    }

    protected override void OnExecute()
    {
        var rootDir = Environment.CurrentDirectory;

        if (Arguments.HasValue(Constants.ArgumentNameRootDirectory) == true)
        {
            rootDir = Arguments.GetStringValue(Constants.ArgumentNameRootDirectory);
        }

        if (Directory.Exists(rootDir))
        {
            throw new KnownException($"Starting directory '{rootDir}' does not exist.");
        }

        CleanDirectory(rootDir);
    }

    public void CleanDirectory(string fromDir)
    {
        DirectoryInfo dirInfo = new DirectoryInfo(fromDir);

        var allDirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);

        var pathSeparator = Path.DirectorySeparatorChar;

        foreach (var dir in allDirs)
        {
            string dirFullNameToLower = dir.FullName.ToLower();

            if (dirFullNameToLower.EndsWith($"{pathSeparator}.git") == true ||
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
