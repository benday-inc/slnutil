using System.Xml;
using System.Xml.Linq;

using Benday.XmlUtilities;

namespace Benday.SolutionUtil.Api;

public static class ProjectUtilities
{
    public static string FindSolution()
    {
        var currentDirectory = Environment.CurrentDirectory;

        return FindSolution(currentDirectory);
    }

    public static string? FindFirstFileName(string startingDirectory, string filename)
    {
        var solutionFile = Directory.GetFiles(startingDirectory, filename, SearchOption.AllDirectories).FirstOrDefault();

        if (string.IsNullOrEmpty(solutionFile) == false)
        {
            return solutionFile;
        }
        else
        {
            return null;
        }
    }

    public static string FindSolution(string startingDirectory)
    {
        var solutionFile = Directory.GetFiles(startingDirectory, "*.sln", SearchOption.AllDirectories).FirstOrDefault();

        if (string.IsNullOrEmpty(solutionFile) == false)
        {
            return solutionFile;
        }
        else
        {
            solutionFile = FindSolutionInParentFolders(startingDirectory);
        }

        return solutionFile;
    }

    private static string FindSolutionInParentFolders(string currentDirectory)
    {
        var dir = new DirectoryInfo(currentDirectory);

        return FindSolutionInParentFolders(dir);
    }

    private static string FindSolutionInParentFolders(DirectoryInfo dir)
    {
        if (dir.Parent == null)
        {
            return null;
        }
        else
        {
            var solution = dir.Parent.GetFiles("*.sln").FirstOrDefault();

            if (solution != null)
            {
                return solution.FullName;
            }
            else
            {
                return FindSolutionInParentFolders(dir.Parent);
            }
        }
    }

    public static List<ReferenceInfo> GetReferenceForProjectFile(string projectFilePath)
    {
        if (Path.GetExtension(projectFilePath) == ".sqlproj")
        {
            return new List<ReferenceInfo>();
        }
        else
        {
            string projectFileContents = File.ReadAllText(projectFilePath);

            var references = GetReferences(projectFileContents);

            return references;
        }
    }

    private static List<ReferenceInfo> GetReferences(string projectFileContents)
    {
        List<ReferenceInfo> returnValues = new List<ReferenceInfo>();

        var root = XElement.Parse(projectFileContents);

        var projectReferences = GetElements(root, "ItemGroup", "ProjectReference");
        var packageReferences = GetElements(root, "ItemGroup", "Reference");

        AddReferenceInfos(returnValues, "project-ref", projectReferences);
        AddReferenceInfos(returnValues, "package-ref", packageReferences);

        return returnValues;
    }

    private static void AddReferenceInfos(List<ReferenceInfo> returnValues, string referenceType,
        List<XElement> referenceElements)
    {
        foreach (var item in referenceElements)
        {
            returnValues.Add(new ReferenceInfo()
            {
                ReferenceType = referenceType,
                ReferenceTarget = item.AttributeValue("Include")
            });
        }
    }

    private static List<XElement> GetElements(XElement root, string elementName1, string elementName2)
    {
        List<XElement> returnValues = new List<XElement>();

        var elements1 = root.ElementsByLocalName(elementName1);

        foreach (var element1 in elements1)
        {
            var elements2 = element1.ElementsByLocalName(elementName2);

            if (elements2 != null && elements2.Count() > 0)
            {
                returnValues.AddRange(elements2);
            }
        }

        return returnValues;
    }

    internal static void AssertFileExists(string path, string description)
    {
        if (File.Exists(path) == false)
        {
            throw new FileNotFoundException(
                $"Could not find file for {description} at '{path}'.", path);
        }
    }

    internal static void AssertDirectoryExists(string path, string description)
    {
        if (Directory.Exists(path) == false)
        {
            throw new DirectoryNotFoundException(
                $"Could not find directory for {description}. {path}");
        }
    }

    public static XElement FindTargetFrameworkElement(string filename, XElement root)
    {
        var propertyGroups =
            root.ElementsByLocalName("PropertyGroup");

        if (propertyGroups == null || propertyGroups.Count() == 0)
        {
            throw new InvalidOperationException(
                $"Could not locate PropertyGroup node in file '{filename}'.");
        }
        else
        {
            XElement? returnValue = null;

            foreach (var propertyGroup in propertyGroups)
            {
                returnValue =
                    propertyGroup.ElementByLocalName(
                        "TargetFramework");

                if (returnValue != null)
                {
                    return returnValue;
                }

                returnValue =
                    propertyGroup.ElementByLocalName(
                        "TargetFrameworkVersion");

                if (returnValue != null)
                {
                    return returnValue;
                }
            }

            if (returnValue == null)
            {
                throw new KnownException(
                    $"Could not find TargetFramework or TargetFrameworkVersion element in file '{filename}'.");
            }
            else
            {
                return returnValue;
            }
        }
    }

    public static string GetFrameworkVersion(string dir, string project)
    {
        var pathToProjectFile = Path.Combine(dir, project);

        if (!File.Exists(pathToProjectFile))
        {
            return "(n/a)";
        }

        var doc = XDocument.Load(pathToProjectFile);

        var root = doc.Root;

        if (root == null || root.Name.LocalName != "Project")
        {
            return "(n/a)";
        }

        var targetFrameworkElement =
            ProjectUtilities.FindTargetFrameworkElement(pathToProjectFile, root);

        if (targetFrameworkElement == null || string.IsNullOrEmpty(targetFrameworkElement.Value) == true)
        {
            return "(n/a)";
        }
        else
        {
            return targetFrameworkElement.Value;
        }
    }
}