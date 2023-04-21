using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Benday.XmlUtilities;
using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameListPackagesLegacy, 
    Description = "Lists packages referenced in legacy style packages.config files.")]
public class ListPackagesLegacyStyleCommand
    : SynchronousCommand
{

    public ListPackagesLegacyStyleCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameRootDirectory).
            WithDescription("Path to start search from").AsNotRequired();

        return args;
    }


    protected override void OnExecute()
    {
        string rootDirPath;

        if (Arguments.HasValue(Constants.ArgumentNameRootDirectory) == false)
        {
            rootDirPath = Environment.CurrentDirectory;
        }
        else
        {
            rootDirPath = Arguments.GetStringValue(Constants.ArgumentNameRootDirectory);
        }

        if (Directory.Exists(rootDirPath) == false)
        {
            throw new KnownException($"Root directory for search does not exist. '{rootDirPath}'");
        }

        var packagesConfigFiles = GetPackageConfigFiles(rootDirPath);

        if (packagesConfigFiles.Length == 0)
        {
            throw new KnownException("No packages.config files found.");
        }
        else
        {
            DisplayPackages(packagesConfigFiles);
        }

    }

    private void DisplayPackages(string[] packagesConfigFiles)
    {
        foreach (var packagesConfigFile in packagesConfigFiles)
        {
            DisplayPackage(packagesConfigFile);
        }
    }

    private void DisplayPackage(string packagesConfigFile)
    {
        try
        {
            var element = XElement.Parse(File.ReadAllText(packagesConfigFile));

            if (element.Name != "packages")
            {
                WriteLine($"File '{packagesConfigFile}' does not have 'packages' as the root node name. It probably isn't a nuget package config file.");
            }
            else
            {
                WriteLine($"{packagesConfigFile}");

                var packages = element.Elements("package");

                if (packages == null || packages.Count() == 0)
                {
                    WriteLine("\t(no packages)");
                }
                else
                {
                    foreach (var package in packages)
                    {
                        WriteLine($"\t{package.AttributeValue("id")} -- {package.AttributeValue("version")} -- {package.AttributeValue("targetFramework")}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            WriteLine($"Problem reading/parsing '{packagesConfigFile}'. It probably isn't a nuget package config file. {ex.Message}");
        }
    }

    internal string[] GetPackageConfigFiles(string rootDir)
    {
        var matches = Directory.GetFiles(
            rootDir, "packages.config", SearchOption.AllDirectories);

        return matches;
    }

    
}

