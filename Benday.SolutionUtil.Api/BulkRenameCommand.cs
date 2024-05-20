using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;



[Command(Name = Constants.CommandArgumentNameRename,
    Description = "Bulk rename for files and folders.")]
public class BulkRenameCommand : SynchronousCommand
{
    public BulkRenameCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameRootDirectory)
            .AsNotRequired()
            .WithDescription("Starting directory for the rename operation");

        args.AddString(Constants.ArgumentNameFromValue)
            .AsRequired()
            .WithDescription("String to search for and replace");

        args.AddString(Constants.ArgumentNameToValue)
            .AsRequired()
            .WithDescription("Replacement value");

        args.AddBoolean(Constants.ArgumentNamePreview)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Preview changes");

        args.AddBoolean(Constants.ArgumentNameRecursive)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Recurse the directory tree");

        return args;
    }

    protected override void OnExecute()
    {
        string sourceDir = Environment.CurrentDirectory;

        if (Arguments.HasValue(Constants.ArgumentNameRootDirectory) == true)
        {
            sourceDir = Arguments.GetStringValue(
                Constants.ArgumentNameRootDirectory);
        }

        Utilities.AssertDirectoryExists(
            sourceDir, Constants.ArgumentNameConfigFilename);

        var fromValue = Arguments.GetStringValue(
            Constants.ArgumentNameFromValue);

        var toValue = Arguments.GetStringValue(
            Constants.ArgumentNameToValue);

        var preview = Arguments.GetBooleanValue(
            Constants.ArgumentNamePreview);

        var recursive = Arguments.GetBooleanValue(
            Constants.ArgumentNameRecursive);

        Rename(sourceDir, fromValue, toValue, preview, recursive);
    }

    private void Rename(string sourceDir, string fromValue, string toValue, bool preview, bool recursive)
    {
        WriteLine($"Starting rename...");
        var dir = new DirectoryInfo(sourceDir);
        RenameFiles(fromValue, toValue, preview, dir, recursive);
        RenameSubdirectories(fromValue, toValue, preview, dir, recursive);
        WriteLine($"Rename complete.");
    }

    private void RenameSubdirectories(
        string fromValue, string toValue, bool preview, DirectoryInfo dir, bool recursive)
    {
        WriteLine($"Starting rename of subdirectories...");
        var searchOption = SearchOption.TopDirectoryOnly;

        if (recursive == true)
        {
            searchOption = SearchOption.AllDirectories;
        }

        var dirs = dir.GetDirectories($"*{fromValue}*", searchOption);

        string toName;
        string toPath;

        foreach (var item in dirs)
        {
            toName = item.Name.Replace(fromValue, toValue);

            if (item.Parent is null)
            {
                continue;
            }

            toPath = Path.Combine(item.Parent.FullName, toName);

            if (preview == true)
            {
                WriteLine(
                    $"PREVIEW: Rename directory {item.Name} to {toName}");
                WriteLine(
                    $"\t{item.FullName} to {toPath}");
            }
            else
            {
                WriteLine(
                    $"Rename {item.Name} directory to {toName}");

                Directory.Move(item.FullName, toPath);
            }
        }

        WriteLine($"Completed rename of subdirectories.");
    }

    private void RenameFiles(string fromValue, string toValue, bool preview, DirectoryInfo dir, bool recursive)
    {
        WriteLine($"Starting rename of files...");

        var searchOption = SearchOption.TopDirectoryOnly;

        if (recursive == true)
        {
            searchOption = SearchOption.AllDirectories;
        }

        var files = dir.GetFiles($"*{fromValue}*", searchOption);

        string toFilename;
        string toFilepath;

        foreach (var item in files)
        {
            toFilename = item.Name.Replace(fromValue, toValue);

            if (item.DirectoryName is null)
            {
                continue;
            }

            toFilepath = Path.Combine(item.DirectoryName, toFilename);

            if (preview == true)
            {
                WriteLine(
                    $"PREVIEW: Rename {item.Name} to {toFilename}");

                WriteLine(
                    $"\t{item.FullName} to {toFilepath}");
            }
            else
            {
                WriteLine(
                    $"Rename {item.Name} to {toFilename}");


                File.Move(item.FullName, toFilepath);
            }
        }

        WriteLine($"Completed rename of files.");
    }
}
