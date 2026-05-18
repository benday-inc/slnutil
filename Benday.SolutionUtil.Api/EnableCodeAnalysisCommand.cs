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
    Description = "Enable Roslyn code analysis across a solution. Default mode creates/merges Directory.Build.props at the solution root. Use --per-project to install analyzers directly into each csproj (required for packages.config projects).")]
public class EnableCodeAnalysisCommand : SynchronousCommand
{
    private const string NetAnalyzersPackageId = "Microsoft.CodeAnalysis.NetAnalyzers";
    private const string CodeStylePackageId = "Microsoft.CodeAnalysis.CSharp.CodeStyle";
    private const string DefaultAnalysisLevel = "latest-Minimum";

    private static readonly IReadOnlyList<string> NetAnalyzersDllPaths = new[]
    {
        Path.Combine("analyzers", "dotnet", "cs", "Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll"),
        Path.Combine("analyzers", "dotnet", "cs", "Microsoft.CodeAnalysis.NetAnalyzers.resources.dll"),
        Path.Combine("analyzers", "dotnet", "Microsoft.CodeAnalysis.NetAnalyzers.dll"),
    };

    private static readonly IReadOnlyList<string> CodeStyleDllPaths = new[]
    {
        Path.Combine("analyzers", "dotnet", "cs", "Microsoft.CodeAnalysis.CSharp.CodeStyle.dll"),
        Path.Combine("analyzers", "dotnet", "cs", "Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes.dll"),
        Path.Combine("analyzers", "dotnet", "cs", "Microsoft.CodeAnalysis.CodeStyle.dll"),
        Path.Combine("analyzers", "dotnet", "cs", "Microsoft.CodeAnalysis.CodeStyle.Fixes.dll"),
    };

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

        args.AddString(Constants.ArgumentNameCodeStyleVersion)
            .AsNotRequired()
            .WithDescription("Version of Microsoft.CodeAnalysis.CSharp.CodeStyle to reference for .NET Framework projects (when --enforce-code-style is enabled). If omitted, queries nuget.org for the latest stable version.");

        args.AddBoolean(Constants.ArgumentNameEnforceCodeStyle)
            .AsNotRequired().AllowEmptyValue()
            .WithDefaultValue(true)
            .WithDescription("Enable code style enforcement (IDE* rules) during build. For .NET 5+ sets EnforceCodeStyleInBuild=true. For .NET Framework installs Microsoft.CodeAnalysis.CSharp.CodeStyle. Default: true.");

        args.AddBoolean(Constants.ArgumentNameDryRun)
            .AsNotRequired().AllowEmptyValue()
            .WithDescription("Preview what would change without writing any files.");

        args.AddBoolean(Constants.ArgumentNameCreateEditorConfig)
            .AsNotRequired().AllowEmptyValue()
            .WithDescription("Also create a starter .editorconfig at the solution root if one doesn't already exist.");

        args.AddBoolean(Constants.ArgumentNamePerProject)
            .AsNotRequired().AllowEmptyValue()
            .WithDescription("Install analyzers into each project individually (modifies csproj + packages.config) instead of using Directory.Build.props. Required for solutions where projects use packages.config.");

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
        var perProject = Arguments.GetBooleanValue(Constants.ArgumentNamePerProject);
        var enforceCodeStyle = Arguments.GetBooleanValue(Constants.ArgumentNameEnforceCodeStyle);

        var analysisLevel = Arguments.GetStringValue(Constants.ArgumentNameAnalysisLevel);
        var analyzerVersion = ResolveNetAnalyzersVersion(hasFrameworkProjects);
        var codeStyleVersion = ResolveCodeStyleVersion(hasFrameworkProjects, enforceCodeStyle);

        var packages = BuildPackagesToInstall(hasFrameworkProjects, analyzerVersion, codeStyleVersion, enforceCodeStyle);

        if (perProject)
        {
            ApplyPerProject(projects, packages, analysisLevel, enforceCodeStyle, solutionDir);
        }
        else
        {
            var propsPath = Path.Combine(solutionDir, "Directory.Build.props");
            WriteDirectoryBuildProps(propsPath, packages, analysisLevel, enforceCodeStyle);
        }

        if (Arguments.GetBooleanValue(Constants.ArgumentNameCreateEditorConfig))
        {
            WriteStarterEditorConfig(Path.Combine(solutionDir, ".editorconfig"));
        }

        if (perProject == false && packagesConfigProjects.Count > 0)
        {
            WriteLine(string.Empty);
            WriteLine($"WARNING: {packagesConfigProjects.Count} project(s) use packages.config and will not pick up the analyzer:");
            foreach (var project in packagesConfigProjects)
            {
                WriteLine($"  - {project.FileName}");
            }
            WriteLine("  Consider migrating these projects to PackageReference format,");
            WriteLine("  or re-run with /per-project:true to install the analyzer directly into each project.");
        }

        WriteLine(string.Empty);
        WriteLine("Next steps:");
        if (perProject && packagesConfigProjects.Count > 0)
        {
            WriteLine("  1. Run `nuget restore` on the solution to download the analyzer DLLs into the packages folder:");
            WriteLine($"       nuget.exe restore \"{solutionPath}\"");
            WriteLine("     (or Visual Studio: Tools -> NuGet Package Manager -> Restore NuGet Packages)");
            WriteLine("  2. Rebuild the solution");
            WriteLine("  3. Check the Error List in VS (make sure Warnings and Messages are toggled on)");
            WriteLine("  4. Adjust rule severities in .editorconfig as needed");
        }
        else
        {
            WriteLine("  1. Restore NuGet packages (dotnet restore or VS -> Restore NuGet Packages)");
            WriteLine("  2. Rebuild the solution");
            WriteLine("  3. Check the Error List in VS (make sure Warnings and Messages are toggled on)");
            WriteLine("  4. Adjust rule severities in .editorconfig as needed");
        }
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

    private string ResolveNetAnalyzersVersion(bool hasFrameworkProjects)
    {
        return ResolvePackageVersion(
            NetAnalyzersPackageId,
            Constants.ArgumentNameAnalyzerVersion,
            needed: hasFrameworkProjects);
    }

    private string ResolveCodeStyleVersion(bool hasFrameworkProjects, bool enforceCodeStyle)
    {
        return ResolvePackageVersion(
            CodeStylePackageId,
            Constants.ArgumentNameCodeStyleVersion,
            needed: hasFrameworkProjects && enforceCodeStyle);
    }

    private string ResolvePackageVersion(string packageId, string versionArgName, bool needed)
    {
        if (Arguments.HasValue(versionArgName))
        {
            return Arguments.GetStringValue(versionArgName);
        }

        if (needed == false)
        {
            return string.Empty;
        }

        WriteLine($"Looking up latest stable version of {packageId} on nuget.org...");

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var url = $"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json";
            var json = client.GetStringAsync(url).GetAwaiter().GetResult();

            using var doc = JsonDocument.Parse(json);
            var versions = doc.RootElement.GetProperty("versions");

            string? latestStable = null;
            foreach (var versionElement in versions.EnumerateArray())
            {
                var version = versionElement.GetString();
                if (string.IsNullOrEmpty(version)) continue;
                if (version.Contains('-')) continue; // skip prerelease
                latestStable = version;
            }

            if (string.IsNullOrEmpty(latestStable))
            {
                throw new KnownException($"Could not find a stable version of {packageId} on nuget.org.");
            }

            WriteLine($"Latest stable version: {latestStable}");
            return latestStable;
        }
        catch (Exception ex) when (ex is not KnownException)
        {
            throw new KnownException(
                $"Failed to query nuget.org for {packageId} version: {ex.Message}. " +
                $"Specify a version explicitly with /{versionArgName}:<version>.");
        }
    }

    private static List<AnalyzerPackageInfo> BuildPackagesToInstall(
        bool hasFrameworkProjects, string analyzerVersion, string codeStyleVersion, bool enforceCodeStyle)
    {
        var packages = new List<AnalyzerPackageInfo>();

        if (hasFrameworkProjects == false)
        {
            return packages; // SDK ships both analyzer packages built-in
        }

        packages.Add(new AnalyzerPackageInfo(NetAnalyzersPackageId, analyzerVersion, NetAnalyzersDllPaths));

        if (enforceCodeStyle)
        {
            packages.Add(new AnalyzerPackageInfo(CodeStylePackageId, codeStyleVersion, CodeStyleDllPaths));
        }

        return packages;
    }

    private void WriteDirectoryBuildProps(string propsPath, List<AnalyzerPackageInfo> packages, string analysisLevel, bool enforceCodeStyle)
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

        foreach (var package in packages)
        {
            EnsurePropsPackageReference(root, package, changes);
        }

        EnsureProperty(root, "RunCodeAnalysis", "false", changes);
        EnsureProperty(root, "EnableNETAnalyzers", "true", changes);
        EnsureProperty(root, "AnalysisLevel", analysisLevel, changes);

        if (enforceCodeStyle)
        {
            // For SDK-style projects this turns on IDE* rule enforcement during build.
            // For old-style/Framework projects it's inert — the CSharp.CodeStyle package handles enforcement directly.
            EnsureProperty(root, "EnforceCodeStyleInBuild", "true", changes);
        }

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

    private static void EnsurePropsPackageReference(XElement projectRoot, AnalyzerPackageInfo package, List<string> changes)
    {
        var existing = projectRoot.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference"
                && string.Equals(e.AttributeValue("Include"), package.PackageId, StringComparison.OrdinalIgnoreCase))
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
            new XAttribute("Include", package.PackageId),
            new XAttribute("Version", package.Version),
            new XElement("PrivateAssets", "all"),
            new XElement("IncludeAssets", "runtime; build; native; contentfiles; analyzers"));

        itemGroup.Add(packageRef);
        changes.Add($"Added PackageReference: {package.PackageId} {package.Version}");
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

    private void ApplyPerProject(List<ProjectScanResult> projects, List<AnalyzerPackageInfo> packages, string analysisLevel, bool enforceCodeStyle, string solutionDir)
    {
        WriteLine(string.Empty);
        var packageSummary = packages.Count == 0
            ? "(SDK built-in analyzers)"
            : string.Join(", ", packages.Select(p => $"{p.PackageId} {p.Version}"));
        var styleNote = enforceCodeStyle ? " (--enforce-code-style enabled)" : " (--enforce-code-style disabled)";
        WriteLine($"{_dryRunPrefix}Installing analyzers per-project{styleNote}...");
        WriteLine($"    Packages: {packageSummary}");
        WriteLine(string.Empty);

        var packagesDir = Path.Combine(solutionDir, "packages");

        foreach (var project in projects)
        {
            WriteLine($"  {project.FileName}:");

            if (string.IsNullOrEmpty(project.TargetFramework))
            {
                WriteLine("    (skipped — could not determine target framework)");
                continue;
            }

            try
            {
                if (project.UsesPackagesConfig)
                {
                    ApplyPackagesConfigProject(project, packages, packagesDir);
                }
                else if (project.IsNetFramework)
                {
                    ApplyFrameworkPackageReferenceProject(project, packages, analysisLevel);
                }
                else
                {
                    ApplySdkProject(project, analysisLevel, enforceCodeStyle);
                }
            }
            catch (Exception ex) when (ex is not KnownException)
            {
                throw new KnownException($"Failed to update '{project.FileName}': {ex.Message}");
            }
        }
    }

    private void ApplyPackagesConfigProject(ProjectScanResult project, List<AnalyzerPackageInfo> packages, string packagesDir)
    {
        if (packages.Count == 0)
        {
            throw new KnownException($"Internal error: no packages to install for packages.config project '{project.FileName}'.");
        }

        var projectDir = Path.GetDirectoryName(project.FullPath)!;
        var packagesConfigPath = Path.Combine(projectDir, "packages.config");

        foreach (var package in packages)
        {
            UpdatePackagesConfigFile(packagesConfigPath, package, project.TargetFramework);
        }

        var csprojChanges = new List<string>();
        var doc = XDocument.Load(project.FullPath);
        var root = doc.Root ?? throw new KnownException($"'{project.FileName}' has no root element.");

        foreach (var package in packages)
        {
            AddAnalyzerEntriesToCsproj(root, projectDir, packagesDir, package, csprojChanges);
        }

        // Old-style csproj only reacts to RunCodeAnalysis (suppresses deprecated FxCopCmd).
        // EnableNETAnalyzers / AnalysisLevel / EnforceCodeStyleInBuild are inert here —
        // the package's own <Analyzer> entries drive analysis.
        SetPropertyForce(root, "RunCodeAnalysis", "false", csprojChanges);

        WriteCsprojIfChanged(doc, project.FullPath, csprojChanges);
    }

    private void ApplyFrameworkPackageReferenceProject(ProjectScanResult project, List<AnalyzerPackageInfo> packages, string analysisLevel)
    {
        if (packages.Count == 0)
        {
            throw new KnownException($"Internal error: no packages to install for framework project '{project.FileName}'.");
        }

        var csprojChanges = new List<string>();
        var doc = XDocument.Load(project.FullPath);
        var root = doc.Root ?? throw new KnownException($"'{project.FileName}' has no root element.");

        foreach (var package in packages)
        {
            AddCsprojPackageReference(root, package, csprojChanges);
        }

        SetPropertyForce(root, "RunCodeAnalysis", "false", csprojChanges);
        SetPropertyForce(root, "EnableNETAnalyzers", "true", csprojChanges);
        SetPropertyForce(root, "AnalysisLevel", analysisLevel, csprojChanges);
        // EnforceCodeStyleInBuild deliberately skipped for Framework projects —
        // the CSharp.CodeStyle package handles enforcement directly (per spec).

        WriteCsprojIfChanged(doc, project.FullPath, csprojChanges);
    }

    private void ApplySdkProject(ProjectScanResult project, string analysisLevel, bool enforceCodeStyle)
    {
        var csprojChanges = new List<string>();
        var doc = XDocument.Load(project.FullPath);
        var root = doc.Root ?? throw new KnownException($"'{project.FileName}' has no root element.");

        SetPropertyForce(root, "RunCodeAnalysis", "false", csprojChanges);
        SetPropertyForce(root, "EnableNETAnalyzers", "true", csprojChanges);
        SetPropertyForce(root, "AnalysisLevel", analysisLevel, csprojChanges);

        if (enforceCodeStyle)
        {
            SetPropertyForce(root, "EnforceCodeStyleInBuild", "true", csprojChanges);
        }

        WriteCsprojIfChanged(doc, project.FullPath, csprojChanges);
    }

    private void UpdatePackagesConfigFile(string packagesConfigPath, AnalyzerPackageInfo package, string targetFramework)
    {
        XDocument doc;
        if (File.Exists(packagesConfigPath))
        {
            doc = XDocument.Load(packagesConfigPath);
            if (doc.Root == null || doc.Root.Name.LocalName != "packages")
            {
                throw new KnownException($"'{packagesConfigPath}' does not have a <packages> root element.");
            }
        }
        else
        {
            doc = new XDocument(new XElement("packages"));
        }

        var root = doc.Root!;

        var existing = root.Elements()
            .Where(e => e.Name.LocalName == "package")
            .FirstOrDefault(e => string.Equals(e.AttributeValue("id"), package.PackageId, StringComparison.OrdinalIgnoreCase));

        var changed = false;

        if (existing != null)
        {
            var existingVersion = existing.AttributeValue("version");
            if (string.Equals(existingVersion, package.Version, StringComparison.OrdinalIgnoreCase))
            {
                WriteLine($"    packages.config: {package.PackageId} {package.Version} already installed");
            }
            else
            {
                existing.SetAttributeValue("version", package.Version);
                existing.SetAttributeValue("targetFramework", targetFramework);
                existing.SetAttributeValue("developmentDependency", "true");
                WriteLine($"    {_dryRunPrefix}packages.config: updated {package.PackageId} {existingVersion} -> {package.Version}");
                changed = true;
            }
        }
        else
        {
            root.Add(new XElement("package",
                new XAttribute("id", package.PackageId),
                new XAttribute("version", package.Version),
                new XAttribute("targetFramework", targetFramework),
                new XAttribute("developmentDependency", "true")));
            WriteLine($"    {_dryRunPrefix}packages.config: added {package.PackageId} {package.Version}");
            changed = true;
        }

        if (changed && _dryRun == false)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using var stream = new FileStream(packagesConfigPath, FileMode.Create, FileAccess.Write);
            using var writer = XmlWriter.Create(stream, settings);
            doc.Save(writer);
        }
    }

    private void AddAnalyzerEntriesToCsproj(XElement projectRoot, string projectDir, string packagesDir, AnalyzerPackageInfo package, List<string> changes)
    {
        var ns = projectRoot.GetDefaultNamespace();
        var packageRoot = Path.Combine(packagesDir, $"{package.PackageId}.{package.Version}");

        XElement? targetItemGroup = null;
        var addedCount = 0;

        foreach (var dllRel in package.AnalyzerDllRelativePaths)
        {
            var absPath = Path.Combine(packageRoot, dllRel);
            var relFromProject = Path.GetRelativePath(projectDir, absPath);

            var alreadyExists = projectRoot.Descendants()
                .Where(e => e.Name.LocalName == "Analyzer")
                .Any(e => string.Equals(e.AttributeValue("Include"), relFromProject, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                continue;
            }

            if (targetItemGroup == null)
            {
                targetItemGroup = projectRoot.ElementsByLocalName("ItemGroup")
                    .FirstOrDefault(g => g.Elements().Any(c => c.Name.LocalName == "Analyzer"));

                if (targetItemGroup == null)
                {
                    targetItemGroup = new XElement(ns + "ItemGroup");
                    projectRoot.Add(targetItemGroup);
                }
            }

            targetItemGroup.Add(new XElement(ns + "Analyzer", new XAttribute("Include", relFromProject)));
            addedCount++;
        }

        if (addedCount > 0)
        {
            changes.Add($"added {addedCount} <Analyzer> entries for {package.PackageId}");
        }
        else
        {
            WriteLine($"    csproj: <Analyzer> entries for {package.PackageId} already present");
        }
    }

    private void AddCsprojPackageReference(XElement projectRoot, AnalyzerPackageInfo package, List<string> changes)
    {
        var existing = projectRoot.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference"
                && string.Equals(e.AttributeValue("Include"), package.PackageId, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (existing != null)
        {
            WriteLine($"    csproj: PackageReference {package.PackageId} already installed");
            return;
        }

        var ns = projectRoot.GetDefaultNamespace();

        var targetItemGroup = projectRoot.ElementsByLocalName("ItemGroup")
            .FirstOrDefault(g => g.Elements().Any(c => c.Name.LocalName == "PackageReference"));

        if (targetItemGroup == null)
        {
            targetItemGroup = new XElement(ns + "ItemGroup");
            projectRoot.Add(targetItemGroup);
        }

        targetItemGroup.Add(new XElement(ns + "PackageReference",
            new XAttribute("Include", package.PackageId),
            new XAttribute("Version", package.Version),
            new XElement(ns + "PrivateAssets", "all"),
            new XElement(ns + "IncludeAssets", "runtime; build; native; contentfiles; analyzers")));

        changes.Add($"added PackageReference: {package.PackageId} {package.Version}");
    }

    private static void SetPropertyForce(XElement projectRoot, string name, string desiredValue, List<string> changes)
    {
        var existing = projectRoot.Descendants()
            .Where(e => e.Name.LocalName == name
                && e.Parent != null
                && e.Parent.Name.LocalName == "PropertyGroup")
            .FirstOrDefault();

        if (existing != null)
        {
            if (string.Equals(existing.Value, desiredValue, StringComparison.Ordinal))
            {
                return;
            }

            var oldValue = existing.Value;
            existing.Value = desiredValue;
            changes.Add($"{name}: {oldValue} -> {desiredValue}");
            return;
        }

        var ns = projectRoot.GetDefaultNamespace();
        var propertyGroup = projectRoot.ElementsByLocalName("PropertyGroup").FirstOrDefault();
        if (propertyGroup == null)
        {
            propertyGroup = new XElement(ns + "PropertyGroup");
            projectRoot.Add(propertyGroup);
        }

        propertyGroup.Add(new XElement(ns + name, desiredValue));
        changes.Add($"set {name} = {desiredValue}");
    }

    private void WriteCsprojIfChanged(XDocument doc, string projectPath, List<string> changes)
    {
        if (changes.Count == 0)
        {
            WriteLine("    csproj: no changes needed");
            return;
        }

        foreach (var change in changes)
        {
            WriteLine($"    {_dryRunPrefix}csproj: {change}");
        }

        if (_dryRun)
        {
            return;
        }

        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = doc.Declaration == null,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var stream = new FileStream(projectPath, FileMode.Create, FileAccess.Write);
        using var writer = XmlWriter.Create(stream, settings);
        doc.Save(writer);
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

    private sealed class AnalyzerPackageInfo
    {
        public string PackageId { get; }
        public string Version { get; }
        public IReadOnlyList<string> AnalyzerDllRelativePaths { get; }

        public AnalyzerPackageInfo(string packageId, string version, IReadOnlyList<string> analyzerDllRelativePaths)
        {
            PackageId = packageId;
            Version = version;
            AnalyzerDllRelativePaths = analyzerDllRelativePaths;
        }
    }
}
