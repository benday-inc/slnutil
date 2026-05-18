using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

using Benday.CommandsFramework;
using Benday.XmlUtilities;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameEnableCodeAnalysis,
    IsAsync = false,
    Description = "Enable Roslyn code analysis across a solution by creating/merging a Directory.Build.props at the solution root.")]
public class EnableCodeAnalysisCommand : SynchronousCommand
{
    private const string AnalyzerPackageId = "Microsoft.CodeAnalysis.NetAnalyzers";
    private const string DefaultAnalysisLevel = "latest-Minimum";

    public EnableCodeAnalysisCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameSolutionPath)
            .AsNotRequired()
            .WithDescription("Solution to update. If omitted, searches the current directory for a .sln or .slnx file.");

        args.AddString(Constants.ArgumentNameAnalysisLevel)
            .AsNotRequired()
            .WithDefaultValue(DefaultAnalysisLevel)
            .WithDescription("Value for the AnalysisLevel MSBuild property. Safe default 'latest-Minimum'. Other values: latest-Default, latest-Recommended, latest-All, latest.");

        args.AddString(Constants.ArgumentNameAnalyzerVersion)
            .AsNotRequired()
            .WithDescription("Version of Microsoft.CodeAnalysis.NetAnalyzers to reference for .NET Framework projects. If omitted, queries nuget.org for the latest stable version.");

        args.AddBoolean(Constants.ArgumentNameDryRun)
            .AsNotRequired().AllowEmptyValue()
            .WithDescription("Preview what would change without writing any files.");

        args.AddBoolean(Constants.ArgumentNameCreateEditorConfig)
            .AsNotRequired().AllowEmptyValue()
            .WithDescription("Also create a starter .editorconfig at the solution root if one doesn't already exist.");

        return args;
    }

    private bool _dryRun;
    private string _dryRunPrefix = string.Empty;

    protected override void OnExecute()
    {
        _dryRun = Arguments.GetBooleanValue(Constants.ArgumentNameDryRun);
        _dryRunPrefix = _dryRun ? "[DRY RUN] " : string.Empty;

        var solutionPath = ResolveSolutionPath();
        var solutionDir = new FileInfo(solutionPath).DirectoryName
            ?? throw new InvalidOperationException("Solution file has a null directory.");

        WriteLine($"Scanning solution: {solutionPath}");
        WriteLine(string.Empty);

        var projects = ScanProjects(solutionPath, solutionDir);

        if (projects.Count == 0)
        {
            WriteLine("No projects found in solution.");
            return;
        }

        PrintProjectSummary(projects);

        var hasFrameworkProjects = projects.Any(p => p.IsNetFramework);
        var packagesConfigProjects = projects.Where(p => p.UsesPackagesConfig).ToList();

        var analysisLevel = Arguments.GetStringValue(Constants.ArgumentNameAnalysisLevel);
        var analyzerVersion = ResolveAnalyzerVersion(hasFrameworkProjects);

        var propsPath = Path.Combine(solutionDir, "Directory.Build.props");
        WriteDirectoryBuildProps(propsPath, hasFrameworkProjects, analyzerVersion, analysisLevel);

        if (Arguments.GetBooleanValue(Constants.ArgumentNameCreateEditorConfig))
        {
            WriteStarterEditorConfig(Path.Combine(solutionDir, ".editorconfig"));
        }

        if (packagesConfigProjects.Count > 0)
        {
            WriteLine(string.Empty);
            WriteLine($"WARNING: {packagesConfigProjects.Count} project(s) use packages.config and will not pick up the analyzer:");
            foreach (var project in packagesConfigProjects)
            {
                WriteLine($"  - {project.FileName}");
            }
            WriteLine("  Consider migrating these projects to PackageReference format.");
            WriteLine("  In Visual Studio: right-click packages.config -> Migrate packages.config to PackageReference.");
        }

        WriteLine(string.Empty);
        WriteLine("Next steps:");
        WriteLine("  1. Restore NuGet packages (dotnet restore or VS -> Restore NuGet Packages)");
        WriteLine("  2. Rebuild the solution");
        WriteLine("  3. Check the Error List in VS (make sure Warnings and Messages are toggled on)");
        WriteLine("  4. Adjust rule severities in .editorconfig as needed");
    }

    private string ResolveSolutionPath()
    {
        string solutionPath;

        if (Arguments.HasValue(Constants.ArgumentNameSolutionPath))
        {
            solutionPath = Arguments[Constants.ArgumentNameSolutionPath].Value;
            ProjectUtilities.AssertFileExists(solutionPath, Constants.ArgumentNameSolutionPath);
        }
        else
        {
            solutionPath = ProjectUtilities.FindSolution()
                ?? throw new KnownException("No solution found in current directory.");
        }

        return solutionPath;
    }

    private void PrintProjectSummary(List<ProjectScanResult> projects)
    {
        WriteLine($"Found {projects.Count} project(s):");

        var nameWidth = Math.Max(20, projects.Max(p => p.FileName.Length) + 2);
        var tfmWidth = Math.Max(8, projects.Max(p => (p.TargetFramework ?? string.Empty).Length) + 2);

        foreach (var project in projects)
        {
            var nugetStyle = project.UsesPackagesConfig ? "packages.config" : "PackageReference";
            var tfm = string.IsNullOrEmpty(project.TargetFramework) ? "(unknown)" : project.TargetFramework;
            WriteLine($"  {project.FileName.PadRight(nameWidth)}{tfm.PadRight(tfmWidth)}{nugetStyle}");
        }

        WriteLine(string.Empty);
    }

    private List<ProjectScanResult> ScanProjects(string solutionPath, string solutionDir)
    {
        var results = new List<ProjectScanResult>();

        foreach (var relativePath in GetProjects(solutionPath))
        {
            var fullPath = Path.Combine(solutionDir, relativePath);
            var fileInfo = new FileInfo(fullPath);

            var tfm = fileInfo.Exists
                ? ProjectUtilities.GetProjectTargetFrameworkShortForm(fullPath)
                : string.Empty;

            var usesPackagesConfig = fileInfo.Exists
                && ProjectUtilities.ProjectUsesPackagesConfig(fullPath);

            results.Add(new ProjectScanResult
            {
                FileName = fileInfo.Name,
                FullPath = fullPath,
                TargetFramework = tfm,
                UsesPackagesConfig = usesPackagesConfig,
                IsNetFramework = IsNetFrameworkTfm(tfm)
            });
        }

        return results;
    }

    internal static bool IsNetFrameworkTfm(string tfm)
    {
        if (string.IsNullOrWhiteSpace(tfm))
        {
            return false;
        }

        // Multi-targeted projects: treat as Framework if ANY entry is Framework
        foreach (var single in tfm.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = single.Trim();

            // net48, net472, net40 (no dot)  -> Framework
            // net5.0, net8.0, net10.0 (with dot) -> modern .NET
            // netstandard*, netcoreapp* -> not Framework
            if (trimmed.StartsWith("net", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.Contains('.'))
            {
                return true;
            }
        }

        return false;
    }

    private string ResolveAnalyzerVersion(bool hasFrameworkProjects)
    {
        if (Arguments.HasValue(Constants.ArgumentNameAnalyzerVersion))
        {
            return Arguments.GetStringValue(Constants.ArgumentNameAnalyzerVersion);
        }

        if (hasFrameworkProjects == false)
        {
            // not needed — SDK ships analyzers built-in
            return string.Empty;
        }

        WriteLine($"Looking up latest stable version of {AnalyzerPackageId} on nuget.org...");

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var url = $"https://api.nuget.org/v3-flatcontainer/{AnalyzerPackageId.ToLowerInvariant()}/index.json";
            var json = client.GetStringAsync(url).GetAwaiter().GetResult();

            using var doc = JsonDocument.Parse(json);
            var versions = doc.RootElement.GetProperty("versions");

            string? latestStable = null;
            foreach (var versionElement in versions.EnumerateArray())
            {
                var version = versionElement.GetString();
                if (string.IsNullOrEmpty(version)) continue;
                if (version.Contains('-')) continue; // skip prerelease (e.g., 9.0.0-preview1)
                latestStable = version;
            }

            if (string.IsNullOrEmpty(latestStable))
            {
                throw new KnownException($"Could not find a stable version of {AnalyzerPackageId} on nuget.org.");
            }

            WriteLine($"Latest stable version: {latestStable}");
            return latestStable;
        }
        catch (Exception ex) when (ex is not KnownException)
        {
            throw new KnownException(
                $"Failed to query nuget.org for {AnalyzerPackageId} version: {ex.Message}. " +
                $"Specify a version explicitly with /{Constants.ArgumentNameAnalyzerVersion}:<version>.");
        }
    }

    private void WriteDirectoryBuildProps(string propsPath, bool includeAnalyzerPackage, string analyzerVersion, string analysisLevel)
    {
        var existed = File.Exists(propsPath);
        XDocument doc;

        if (existed)
        {
            try
            {
                doc = XDocument.Load(propsPath);
            }
            catch (Exception ex)
            {
                throw new KnownException($"Failed to parse existing Directory.Build.props at '{propsPath}': {ex.Message}");
            }

            if (doc.Root == null || doc.Root.Name.LocalName != "Project")
            {
                throw new KnownException($"Existing Directory.Build.props at '{propsPath}' does not have a <Project> root element.");
            }
        }
        else
        {
            doc = new XDocument(new XElement("Project"));
        }

        var root = doc.Root!;
        var changes = new List<string>();

        if (includeAnalyzerPackage)
        {
            EnsureAnalyzerPackageReference(root, analyzerVersion, changes);
        }

        EnsureProperty(root, "RunCodeAnalysis", "false", changes);
        EnsureProperty(root, "EnableNETAnalyzers", "true", changes);
        EnsureProperty(root, "AnalysisLevel", analysisLevel, changes);

        var verb = existed ? "Updated" : "Created";
        WriteLine($"{_dryRunPrefix}{verb}: {propsPath}");

        if (changes.Count == 0)
        {
            WriteLine("  (no changes — all required entries already present)");
        }
        else
        {
            foreach (var change in changes)
            {
                WriteLine($"  - {change}");
            }
        }

        if (_dryRun == false && changes.Count > 0)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using var stream = new FileStream(propsPath, FileMode.Create, FileAccess.Write);
            using var writer = XmlWriter.Create(stream, settings);
            doc.Save(writer);
        }
    }

    private static void EnsureAnalyzerPackageReference(XElement projectRoot, string version, List<string> changes)
    {
        var existing = projectRoot.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference"
                && string.Equals(e.AttributeValue("Include"), AnalyzerPackageId, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (existing != null)
        {
            return; // honor the existing entry as-is, don't change version or assets
        }

        var itemGroup = projectRoot.ElementsByLocalName("ItemGroup").FirstOrDefault();
        if (itemGroup == null)
        {
            itemGroup = new XElement("ItemGroup");
            projectRoot.AddFirst(itemGroup);
        }

        var packageRef = new XElement("PackageReference",
            new XAttribute("Include", AnalyzerPackageId),
            new XAttribute("Version", version),
            new XElement("PrivateAssets", "all"),
            new XElement("IncludeAssets", "runtime; build; native; contentfiles; analyzers"));

        itemGroup.Add(packageRef);
        changes.Add($"Added PackageReference: {AnalyzerPackageId} {version}");
    }

    private static void EnsureProperty(XElement projectRoot, string propertyName, string propertyValue, List<string> changes)
    {
        var existing = projectRoot.Descendants()
            .Where(e => e.Name.LocalName == propertyName
                && e.Parent != null
                && e.Parent.Name.LocalName == "PropertyGroup")
            .FirstOrDefault();

        if (existing != null)
        {
            return; // don't overwrite user value
        }

        var propertyGroup = projectRoot.ElementsByLocalName("PropertyGroup").FirstOrDefault();
        if (propertyGroup == null)
        {
            propertyGroup = new XElement("PropertyGroup");
            projectRoot.Add(propertyGroup);
        }

        propertyGroup.Add(new XElement(propertyName, propertyValue));
        changes.Add($"Set {propertyName} = {propertyValue}");
    }

    private void WriteStarterEditorConfig(string editorConfigPath)
    {
        if (File.Exists(editorConfigPath))
        {
            WriteLine(string.Empty);
            WriteLine($"WARNING: .editorconfig already exists at {editorConfigPath}. Leaving it alone.");
            return;
        }

        WriteLine(string.Empty);
        WriteLine($"{_dryRunPrefix}Created: {editorConfigPath}");

        if (_dryRun)
        {
            return;
        }

        var content = new StringBuilder();
        content.AppendLine("# Code analysis severity configuration");
        content.AppendLine("[*.cs]");
        content.AppendLine();
        content.AppendLine("# Start with most rules as suggestions rather than warnings");
        content.AppendLine("# to avoid breaking CI on existing codebases.");
        content.AppendLine("# Ratchet up severity over time as violations are addressed.");
        content.AppendLine();
        content.AppendLine("# Design rules");
        content.AppendLine("dotnet_diagnostic.CA1002.severity = suggestion");
        content.AppendLine("dotnet_diagnostic.CA1051.severity = suggestion");
        content.AppendLine();
        content.AppendLine("# Performance rules");
        content.AppendLine("dotnet_diagnostic.CA1822.severity = suggestion");
        content.AppendLine("dotnet_diagnostic.CA1860.severity = suggestion");
        content.AppendLine();
        content.AppendLine("# Reliability rules");
        content.AppendLine("dotnet_diagnostic.CA2007.severity = suggestion");
        content.AppendLine();
        content.AppendLine("# Usage rules");
        content.AppendLine("dotnet_diagnostic.CA2211.severity = suggestion");

        File.WriteAllText(editorConfigPath, content.ToString());
    }

    private static List<string> GetProjects(string solutionPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add("sln");
        startInfo.ArgumentList.Add(solutionPath);
        startInfo.ArgumentList.Add("list");

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Process.Start() returned null.");

        process.WaitForExit();

        var returnValues = new List<string>();
        var line = process.StandardOutput.ReadLine();
        var lineNumber = 0;

        while (line != null)
        {
            if (lineNumber > 1)
            {
                returnValues.Add(line);
            }
            lineNumber++;
            line = process.StandardOutput.ReadLine();
        }

        return returnValues;
    }

    private class ProjectScanResult
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = string.Empty;
        public bool UsesPackagesConfig { get; set; }
        public bool IsNetFramework { get; set; }
    }
}
