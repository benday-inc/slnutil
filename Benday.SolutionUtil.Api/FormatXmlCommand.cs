
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

using Benday.CommandsFramework;
using Benday.Common;
using Benday.XmlUtilities;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameFormatXml,
    IsAsync = false,
    Description = "Formats XML files")]
public class FormatXmlCommand : SynchronousCommand
{
    public FormatXmlCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameFilename)
            .AsRequired()
            .FromPositionalArgument(1)
            .WithDescription("Path to file or wildcard to files");

        args.AddBoolean(Constants.ArgumentNameRecursive)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription("Apply to matching files recursively");

        args.AddBoolean(Constants.ArgumentNameWriteToFile)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription("Write changes back to the file");

        return args;
    }

    protected override void OnExecute()
    {
        var filepath = Arguments.GetStringValue(Constants.ArgumentNameFilename);
        var recursive = Arguments.GetBooleanValue(Constants.ArgumentNameRecursive);
        var writeToFile = Arguments.GetBooleanValue(Constants.ArgumentNameWriteToFile);

        var target = new TargetFileInfo(filepath);
        
        if (target.HasWildcard == true)
        {
            var files = Directory.GetFiles(
                target.DirectoryPath, target.FileName,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                throw new KnownException($"No files found matching '{filepath}'.");
            }

            foreach (var file in files)
            {
                FormatFile(file, writeToFile);
            }
        }
        else
        {
            FormatFile(filepath, writeToFile);
        }
    }

    private void FormatFile(string file, bool writeToFile)
    {
        WriteLine($"Formatting '{file}'...");

        var xml = File.ReadAllText(file);

        var formatter = new Benday.XmlUtilities.XmlFormatter();

        var result = formatter.Format(xml);

        if (writeToFile == false)
        {
            WriteLine(result.Formatted);
        }
        else
        {
            File.WriteAllText(file, result.Formatted);
        }
    }
}
