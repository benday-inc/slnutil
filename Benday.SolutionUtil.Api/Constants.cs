using Benday.CommandsFramework;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace Benday.SolutionUtil.Api;

public static class Constants
{
    public const string ExeName = "slnutil";

    public const string CommandArgumentNameFindSolutions = "findsolutions";
    public const string CommandArgumentNameListSolutionProjects = "listsolutionprojects";
    public const string ArgumentNameRootDirectory = "rootdir";
    public const string ArgumentNameCommaSeparatedValues = "csv";
    public const string ArgumentNameSkipReferences = "skipreferences";
    public const string ArgumentNameListProjects = "listprojects";
    public const string CommandArgumentNameCleanReferences = "cleanreferences";
    public const string ArgumentNameSolutionPath = "solutionpath";
    public const string ArgumentNamePreview = "preview";
    public const string CommandArgumentNameToBase64String = "base64";
    public const string CommandArgumentNameDevTreeClean = "devtreeclean";
    public const string CommandArgumentNameSetConnectionString = "setconnectionstring";


    public const string ArgumentNameValue = "value";
    public const string ArgumentNameConfigFilename = "filename";
    public const string ArgumentNameConnectionStringName = "name";
}
