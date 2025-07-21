
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

using Benday.CommandsFramework;
using Benday.Common;
using Benday.XmlUtilities;

namespace Benday.SolutionUtil.Api;



[Command(Name = Constants.CommandArgumentNameSortReferences,
    IsAsync = false,
    Description = "Sorts the references in an item group for a csproj file.")]
public class SortReferencesCommand : SynchronousCommand
{

    public SortReferencesCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }


    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameProjectName)
            .AsNotRequired()
            .WithDescription("Project file to edit. If this value is not supplied, the tool looks for a csproj file in the current directory.")
            .FromPositionalArgument(1);

        return args;
    }

    protected override void OnExecute()
    {
        string projectPath;

        if (Arguments.HasValue(Constants.ArgumentNameProjectName) == true)
        {
            projectPath = Arguments[Constants.ArgumentNameProjectName].Value;
            ProjectUtilities.AssertFileExists(projectPath, Constants.ArgumentNameProjectName);
        }
        else
        {
            // find a *.csproj file in the current directory

            // and use that as the project path

            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            var files = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                throw new KnownException("No project file found");
            }
            if (files.Length > 1)
            {
                throw new KnownException("More than one project file found");
            }
            projectPath = files[0].FullName;
            WriteLine($"Using project file: {projectPath}");
        }

        SortItemGroupItems(projectPath);

        WriteLine("Done.");
    }

    private void SortItemGroupItems(string projectPath)
    {
        var projectFile = new FileInfo(projectPath);
        var projectFolder = projectFile.DirectoryName ?? throw new InvalidOperationException("Project file does not have a directory name.");

        var projectXml = XDocument.Load(projectPath);

        var itemGroup = projectXml.Descendants("ItemGroup").FirstOrDefault();

        if (itemGroup == null)
        {
            return;
        }

        var elements = itemGroup.Elements().OrderBy(x => x.Name.LocalName).ThenBy(e => e.AttributeValue("Include")); ;

        itemGroup.ReplaceAll(elements);

        projectXml.Save(projectPath);

        WriteLine($"Sorted the first item group elements in {projectPath}");
        WriteLine($"Project file saved to {projectPath}");
    }

}
