
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

using Benday.CommandsFramework;
using Benday.Common;
using Benday.XmlUtilities;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameTouchFile,
    IsAsync = false,
    Description = "Modifies a file's date to current date time or creates a new empty file if it doesn't exist.")]
public class TouchFileCommand : SynchronousCommand
{
    public TouchFileCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameFilename)
            .AsRequired()
            .FromPositionalArgument(1)
            .WithDescription("Path to file");

        return args;
    }

    protected override void OnExecute()
    {
        var filepath = Arguments.GetStringValue(Constants.ArgumentNameFilename);

        filepath = Path.GetFullPath(filepath);

        var dirpath = Path.GetDirectoryName(filepath);

        if (Directory.Exists(dirpath) == false)
        {
            throw new KnownException($"Directory '{dirpath}' does not exist for file '{filepath}'.");
        }

        if (File.Exists(filepath) == false)
        {
            File.WriteAllText(filepath, String.Empty);
        }
        else
        {
            File.SetLastWriteTimeUtc(filepath, DateTime.UtcNow);
        }
    }
}