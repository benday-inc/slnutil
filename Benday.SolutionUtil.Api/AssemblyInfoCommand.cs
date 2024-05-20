
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameAssemblyInfo,
    Description = "View assembly info for a DLL.")]
public class AssemblyInfoCommand : SynchronousCommand
{
    public AssemblyInfoCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameFilename)
            .AsRequired()
            .WithDescription("Assembly to view")
            .FromPositionalArgument(1);

        return args;
    }

    protected override void OnExecute()
    {
        var filename = Arguments.GetPathToFile(
            Constants.ArgumentNameFilename, true);

        ViewAssemblyInfo(filename);
    }

    private void ViewAssemblyInfo(string filename)
    {
        WriteLine();

        var fileInfo = new System.IO.FileInfo(filename);

        if (fileInfo.Exists == false)
        {
            throw new KnownException(
                $"File not found: '{filename}'");
        }

        var fullPath = Path.GetFullPath(filename);

        WriteLine($"Viewing assembly info for '{fileInfo.Name}'...");
        WriteLine();

        WriteLine($"File create date:");
        WriteLine($"{fileInfo.CreationTime}");
        WriteLine();
        
        WriteLine($"File last write date:");
        WriteLine($"{fileInfo.LastWriteTime}");
        WriteLine();
        
        WriteLine($"File size:");
        WriteLine($"{fileInfo.Length} bytes");
        WriteLine();

        var assembly = Assembly.LoadFrom(fullPath);

        var builder = new StringBuilder();

        builder.AppendLine($"Full Path:");
        builder.AppendLine($"{fullPath}");
        builder.AppendLine();


        builder.AppendLine($"Assembly:");
        builder.AppendLine($"{assembly.FullName}");
        builder.AppendLine();

        var attributes = assembly.GetCustomAttributesData().OrderBy(x => x.AttributeType.Name);

        foreach (var attribute in attributes)
        {
            builder.AppendLine($"{FormatName(attribute.AttributeType.Name)}:");

            LogAttributeProperties(builder, attribute);

            builder.AppendLine();
        }

        WriteLine(builder.ToString());
    }

    private string FormatName(string name)
    {
        // split by uppercase letters
        var parts = Regex.Split(name, @"(?<!^)(?=[A-Z])");

        var returnValue = string.Join(" ", parts);

        return returnValue;        
    }


    private void LogAttributeProperties(
        StringBuilder builder, CustomAttributeData attribute)
    {
        attribute.ConstructorArguments.ToList().ForEach(arg =>
        {
            builder.AppendLine($"{arg.Value}");
        });
    }

    /*
    CompilationRelaxationsAttribute:

RuntimeCompatibilityAttribute:
	WrapNonExceptionThrows: True

DebuggableAttribute:

TargetFrameworkAttribute:
	FrameworkDisplayName: .NET 8.0

AssemblyCompanyAttribute:

AssemblyConfigurationAttribute:

AssemblyDescriptionAttribute:

AssemblyFileVersionAttribute:

AssemblyInformationalVersionAttribute:

AssemblyProductAttribute:

AssemblyTitleAttribute:


    */

}
