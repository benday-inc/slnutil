using System.Xml;
using System.Xml.Linq;

using Benday.CommandsFramework;

using Benday.XmlUtilities;

namespace Benday.SolutionUtil.Api;

public static class ProjectUtilities
{
    public static string? FindSolution()
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

    public static string? FindSolution(string startingDirectory)
    {
        // Prefer .slnx files over .sln files
        var solutionFile = Directory.GetFiles(startingDirectory, "*.slnx", SearchOption.AllDirectories).FirstOrDefault();

        if (string.IsNullOrEmpty(solutionFile) == false)
        {
            return solutionFile;
        }

        solutionFile = Directory.GetFiles(startingDirectory, "*.sln", SearchOption.AllDirectories).FirstOrDefault();

        if (string.IsNullOrEmpty(solutionFile) == false)
        {
            return solutionFile;
        }

        solutionFile = FindSolutionInParentFolders(startingDirectory);

        return solutionFile;
    }

    private static string? FindSolutionInParentFolders(string currentDirectory)
    {
        var dir = new DirectoryInfo(currentDirectory);

        return FindSolutionInParentFolders(dir);
    }

    private static string? FindSolutionInParentFolders(DirectoryInfo dir)
    {
        if (dir.Parent == null)
        {
            return null;
        }
        else
        {
            // Prefer .slnx files over .sln files
            var solution = dir.Parent.GetFiles("*.slnx").FirstOrDefault();

            if (solution != null)
            {
                return solution.FullName;
            }

            solution = dir.Parent.GetFiles("*.sln").FirstOrDefault();

            if (solution != null)
            {
                return solution.FullName;
            }

            return FindSolutionInParentFolders(dir.Parent);
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

        try
        {
            var root = XElement.Parse(projectFileContents);

            var projectReferences = GetElements(root, "ItemGroup", "ProjectReference");
            var packageReferenceElements = GetElements(root, "ItemGroup", "PackageReference");
            var oldStyleReferenceElements = GetElements(root, "ItemGroup", "Reference");

            var frameworkRefs = new List<XElement>();
            var nugetViaPackagesConfigRefs = new List<XElement>();
            var binaryRefs = new List<XElement>();

            foreach (var oldStyleRef in oldStyleReferenceElements)
            {
                var hintPathElement = oldStyleRef.ElementByLocalName("HintPath");

                if (hintPathElement == null)
                {
                    // <Reference Include="System" /> style — GAC / framework assembly
                    frameworkRefs.Add(oldStyleRef);
                }
                else if (HintPathPointsIntoPackagesFolder(hintPathElement.Value))
                {
                    nugetViaPackagesConfigRefs.Add(oldStyleRef);
                }
                else
                {
                    binaryRefs.Add(oldStyleRef);
                }
            }

            AddReferenceInfos(returnValues, "project-ref", projectReferences);
            AddReferenceInfos(returnValues, "package-ref", packageReferenceElements);
            AddReferenceInfos(returnValues, "framework-ref", frameworkRefs);
            AddReferenceInfos(returnValues, "nuget-via-packages-config", nugetViaPackagesConfigRefs);
            AddReferenceInfos(returnValues, "binary-ref", binaryRefs);

            return returnValues;
        }
        catch
        {
            return returnValues;
        }
    }

    public static bool HintPathPointsIntoPackagesFolder(string? hintPath)
    {
        if (string.IsNullOrWhiteSpace(hintPath))
        {
            return false;
        }

        var segments = hintPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            if (string.Equals(segment, "packages", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddReferenceInfos(List<ReferenceInfo> returnValues, string referenceType,
        List<XElement> referenceElements)
    {
        foreach (var item in referenceElements)
        {
            if (item.ElementByLocalName("HintPath") != null)
            {
                returnValues.Add(new ReferenceInfo()
                {
                    ReferenceType = referenceType,
                    ReferenceTarget = item.ElementValue("HintPath") ?? string.Empty
                });
            }
            else
            {
                returnValues.Add(new ReferenceInfo()
                {
                    ReferenceType = referenceType,
                    ReferenceTarget = item.AttributeValue("Include")
                });
            }
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

    public static XElement? FindTargetFrameworkElement(
        string filename, XElement root, bool throwExceptionIfNotFound = true)
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
                if (throwExceptionIfNotFound == true)
                {
                    throw new KnownException(
                    $"Could not find TargetFramework or TargetFrameworkVersion element in file '{filename}'.");
                }
                else
                {
                    return null;
                }                
            }
            else
            {
                return returnValue;
            }
        }
    }

    public static OperationResult<XElement>? SetProjectPropertyElement(string filename, XElement root, string propertyName, string propertyValue)
    {
        var propertyGroups =
            root.ElementsByLocalName("PropertyGroup");

        if (propertyGroups == null || propertyGroups.Count() == 0)
        {
            root.Add(new XElement("PropertyGroup"));
            propertyGroups =
                root.ElementsByLocalName("PropertyGroup");
        }

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
                        propertyName);

                if (returnValue != null)
                {
                    if (returnValue.Value != propertyValue)
                    {
                        returnValue.Value = propertyValue;

                        return new OperationResult<XElement>(returnValue, true);
                    }
                    else
                    {
                        return new OperationResult<XElement>(returnValue, false);
                    }
                }
            }

            if (returnValue == null)
            {
                var propertyGroup = propertyGroups.First();

                returnValue = new XElement(propertyName, propertyValue);

                propertyGroup.Add(returnValue);                

                return new OperationResult<XElement>(returnValue, true);
            }

            return null;
        }
    }

    public static bool ProjectUsesPackagesConfig(string projectFilePath)
    {
        var projectDir = Path.GetDirectoryName(projectFilePath);

        if (string.IsNullOrEmpty(projectDir))
        {
            return false;
        }

        return File.Exists(Path.Combine(projectDir, "packages.config"));
    }

    public static string GetProjectTargetFrameworkShortForm(string projectFilePath)
    {
        if (File.Exists(projectFilePath) == false)
        {
            return string.Empty;
        }

        try
        {
            var doc = XDocument.Load(projectFilePath);
            var root = doc.Root;

            if (root == null || root.Name.LocalName != "Project")
            {
                return string.Empty;
            }

            var propertyGroups = root.ElementsByLocalName("PropertyGroup");

            foreach (var propertyGroup in propertyGroups)
            {
                var sdkStyle = propertyGroup.ElementByLocalName("TargetFramework");

                if (sdkStyle != null && string.IsNullOrWhiteSpace(sdkStyle.Value) == false)
                {
                    return sdkStyle.Value.Trim();
                }

                var sdkStyleMulti = propertyGroup.ElementByLocalName("TargetFrameworks");

                if (sdkStyleMulti != null && string.IsNullOrWhiteSpace(sdkStyleMulti.Value) == false)
                {
                    return sdkStyleMulti.Value.Trim();
                }

                var oldStyle = propertyGroup.ElementByLocalName("TargetFrameworkVersion");

                if (oldStyle != null && string.IsNullOrWhiteSpace(oldStyle.Value) == false)
                {
                    return NormalizeFrameworkVersionToShortForm(oldStyle.Value);
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string NormalizeFrameworkVersionToShortForm(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        var trimmed = version.Trim();

        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            // v4.8 → net48, v4.7.2 → net472
            var digitsOnly = trimmed.Substring(1).Replace(".", string.Empty);
            return $"net{digitsOnly}";
        }

        return trimmed;
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
            ProjectUtilities.FindTargetFrameworkElement(pathToProjectFile, root, false);

        if (targetFrameworkElement == null || string.IsNullOrEmpty(targetFrameworkElement.Value) == true)
        {
            return "(n/a)";
        }
        else
        {
            return targetFrameworkElement.Value;
        }
    }
    public static string GetProjectVersion(string projectPath)
    {
        var doc = XDocument.Load(projectPath);

        var root = doc.Root;

        if (root == null || root.Name.LocalName != "Project")
        {
            throw new InvalidOperationException(
                $"Could not find root node in file '{projectPath}'.");
        }

        var assemblyVersion =
            ProjectUtilities.GetProjectPropertyValue(
                projectPath, root, "AssemblyVersion");

        var assemblyVersionString = string.Empty;

        if (assemblyVersion == null || 
            string.IsNullOrEmpty(assemblyVersion.Value) == true)
        {
            
        }
        else
        {
            assemblyVersionString = assemblyVersion.Value;
        }

        if (string.IsNullOrWhiteSpace(assemblyVersionString) == false)
        {
            return assemblyVersionString;
        }


        var packageVersion =
            ProjectUtilities.GetProjectPropertyValue(
                projectPath, root, "Version");

        var packageVersionString = string.Empty;

        if (packageVersion == null ||
            string.IsNullOrEmpty(packageVersion.Value) == true)
        {

        }
        else
        {
            packageVersionString = packageVersion.Value;
        }

        return packageVersionString;

    }

    public static XElement? GetProjectPropertyValue(
        string filename, XElement root, 
        string propertyName)
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
                        propertyName);

                if (returnValue != null)
                {
                    return returnValue;
                }
            }

            if (returnValue == null)
            {
                return null;
            }
            else
            {
                return returnValue;
            }
        }
    }
    public static string IncrementVersion(string currentValue)
    {
        if (string.IsNullOrEmpty(currentValue) == false)
        {
            var tokens = currentValue.Split('.');

            if (tokens.Length < 2)
            {

            }
            else
            {
                var minorVersionAsString = tokens[1];

                if (Int32.TryParse(minorVersionAsString, out var valueAsInt) == true)
                {
                    valueAsInt++;

                    tokens[1] = valueAsInt.ToString();

                    var returnValue = string.Join(".", tokens);

                    return returnValue;
                }                
            }
        }

        return currentValue;
    }
}
