using System.Reflection;
using System.Text;

using Benday.CommandsFramework;

using Microsoft.IdentityModel.Tokens;
namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameClassDiagram,
    Description = "Generate a class diagram for an assembly.")]
public class ClassDiagramCommand : SynchronousCommand
{
    public ClassDiagramCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentsFilterByNamespace)
            .AsNotRequired()
            .WithDescription("Filter by namespace")
            .WithDefaultValue(string.Empty);

        args.AddString(Constants.ArgumentNameFilename)
            .AsRequired()
            .WithDescription("Path to assembly that you want a class diagram for.")
            .FromPositionalArgument(1);

        args.AddBoolean(Constants.ArgumentNameHideInheritance)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Hide inheritance relationships.")
            .WithDefaultValue(false);

        args.AddBoolean(Constants.ArgumentsFilterByTypeNamesModeExact)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Exact match for type names.  Default is contains.")
            .WithDefaultValue(false);

        args.AddString(Constants.ArgumentsFilterByTypeNames)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Show types that exist in this comma separated list. Default search is contains match that matches by substring.")
            .WithDefaultValue(string.Empty);

        return args;
    }

    private Type[] GetTypesSafe(Assembly assembly)
    {
        try
        {
            var assemblyLocation = assembly.Location;

            

            if (assemblyLocation == null)
            {
                return assembly.GetTypes();
            }
            else
            {
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);

                if (assemblyDir == null)
                {
                    return assembly.GetTypes();
                }
                else
                {
                    string nugetCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

                    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                    {
                        var result = TryToResolveDependency(args, assemblyLocation, nugetCachePath);

                        return result;
                    };

                    return assembly.GetTypes();
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            // If a TypeLoadException occurs, return the types that were successfully loaded
            var loadedTypes = ex.Types.Where(t => t != null).ToArray();

            if (loadedTypes.Length == 0)
            {
                return Array.Empty<Type>();
            }
            else
            {
                // return returnValue as array of non-null types

                var returnValue = new List<Type>();

                foreach (var item in loadedTypes)
                {
                    if (item != null)
                    {
                        returnValue.Add(item);
                    }
                }

                return returnValue.ToArray();
            }
        }
    }

    private static Assembly? TryToResolveDependency(ResolveEventArgs args, string assemblyLocation, string nugetCachePath)
    {
        // Parse the assembly name being requested
        var assemblyInfo = new AssemblyName(args.Name);

        if (assemblyInfo == null)
        {
            return null;
        }

        var assemblyName = assemblyInfo.Name;
        var assemblyPath = Path.Combine(assemblyLocation, assemblyName + ".dll");

        // Check if the assembly exists in the hint path
        if (File.Exists(assemblyPath))
        {
            // Load and return the assembly from the specified path
            return Assembly.LoadFrom(assemblyPath);
        }
        else if (assemblyInfo != null && string.IsNullOrEmpty(assemblyInfo.Name) == false)
        {
            string packageName = assemblyInfo.Name.ToLowerInvariant();
            string version = assemblyInfo.Version?.ToString() ?? "";

            var packagePath = Path.Combine(nugetCachePath, packageName);

            if (Directory.Exists(packagePath) == false && Directory.Exists(nugetCachePath) == false)
            {
                return null;
            }
            else if (Directory.Exists(packagePath) == false)
            {
                var dllToFind = packageName + ".dll";

                // find matching files in the nuget cache
                var dllFiles = Directory.GetFiles(nugetCachePath, dllToFind, SearchOption.AllDirectories);

                if (dllFiles.Length == 0)
                {
                    return null;
                }
                else
                {
                    // find all the files that have "netstandard2.0" in the path
                    var netStandardDirs = dllFiles.Where(x => x.Contains("netstandard2.0")).ToArray();

                    if (netStandardDirs.Length == 0)
                    {
                        return null;
                    }
                    else
                    {
                        // sort the directories reverse alphabetically
                        Array.Sort(netStandardDirs, (x, y) => string.Compare(y, x, StringComparison.Ordinal));

                        // find the first directory that has a .dll file with the same name as the assembly
                        foreach (var matchingDll in netStandardDirs)
                        {
                            if (File.Exists(matchingDll))
                            {
                                return Assembly.LoadFrom(matchingDll);
                            }
                        }
                    }
                }
            }
            else
            {
                assemblyPath = Path.Combine(packagePath, version, "lib", "netstandard2.0", packageName + ".dll");

                // If the assembly exists in the NuGet cache, load and return it
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
                else
                {
                    // find any directory in the packagepath named netstandard2.0
                    var netStandardDirs = Directory.GetDirectories(packagePath, "netstandard2.0", SearchOption.AllDirectories);

                    // sort the directories reverse alphabetically
                    Array.Sort(netStandardDirs, (x, y) => string.Compare(y, x, StringComparison.Ordinal));

                    // find the first directory that has a .dll file with the same name as the assembly
                    foreach (var netStandardDir in netStandardDirs)
                    {
                        var dllFiles = Directory.GetFiles(netStandardDir, "*.dll", SearchOption.AllDirectories);

                        foreach (var dllFile in dllFiles)
                        {
                            var dllFilename = Path.GetFileName(dllFile);

                            if (dllFilename.Equals(assemblyInfo.Name + ".dll", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                return Assembly.LoadFrom(dllFile);
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    protected override void OnExecute()
    {
        var filename = Arguments.GetPathToFile(Constants.ArgumentNameFilename, true);

        filename = Path.GetFullPath(filename);

        var filterByNamespace = Arguments.GetStringValue(Constants.ArgumentsFilterByNamespace);
        var typeNameFilterValue = Arguments.GetStringValue(Constants.ArgumentsFilterByTypeNames);
        var typeNameExactMatch = Arguments.GetBooleanValue(Constants.ArgumentsFilterByTypeNamesModeExact);

        var filterByTypeNames = typeNameFilterValue.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // make all the names lowercase for case-insensitive comparison
        filterByTypeNames = filterByTypeNames.Select(x => x.ToLower()).ToArray();

        var hideinheritance = Arguments.GetBooleanValue(Constants.ArgumentNameHideInheritance);

        var assembly = Assembly.LoadFile(filename);

        var assemblyName = assembly.FullName ?? string.Empty;

        var types = GetTypesSafe(assembly);

        var classes = types.Where(t => t.IsClass == true).ToArray();
        var interfaces = types.Where(t => t.IsInterface == true).ToArray();

        var builder = new StringBuilder();

        if (hideinheritance == false)
        {
            foreach (var type in types)
            {
               
                if (type.BaseType != null)
                {
                    if (MatchesFilter(type.BaseType, filterByNamespace, filterByTypeNames, typeNameExactMatch) == true &&
                        classes.Contains(type.BaseType) == true)
                    {
                        builder.AppendLine($"{GetName(type.BaseType)} <|-- {GetName(type)}");
                    }
                }

                var interfacesImplemented = type.GetInterfaces();

                foreach (var interfaceType in interfacesImplemented)
                {
                    if (MatchesFilter(interfaceType, filterByNamespace, filterByTypeNames, typeNameExactMatch) == true &&
                        interfaces.Contains(interfaceType) == true)
                    {
                        builder.AppendLine($"{GetName(interfaceType)} <|.. {GetName(type)}");
                    }
                }
            }
        }

        var lastType = types.Last();

        foreach (var type in types)
        {
            if (MatchesFilter(type, filterByNamespace, filterByTypeNames, typeNameExactMatch) == false)
            {
                continue;
            }

            AddTypeToDiagram(builder, type);
        }

        var tempDir = System.IO.Path.GetTempPath();
        var outputDir = System.IO.Path.Combine(tempDir, "ClassDiagram");
        var assemblyFilename = System.IO.Path.GetFileNameWithoutExtension(filename);
        var outputFilename = System.IO.Path.Combine(outputDir, assemblyFilename + ".html");

        if (System.IO.Directory.Exists(outputDir) == false)
        {
            System.IO.Directory.CreateDirectory(outputDir);
        }

        WriteDiagramToFile(builder, outputFilename, assemblyName, filterByNamespace);

        OpenFileInBrowser(outputFilename);
    }

    private bool MatchesFilter(Type type, string filterByNamespace, string[] filterByTypeNames, bool typeNameExactMatch)
    {
        var returnValue = true;

        if (type == null)
        {
            return false;
        }

        if (type.IsNotPublic == true)
        {
            WriteLine($"Skipping {type.FullName} because it is not public.");

            returnValue = false;
        }

        if (type.IsNestedPrivate == true)
        {
            WriteLine($"Skipping {type.FullName} because it is nested private.");

            returnValue = false;
        }

        if (string.IsNullOrWhiteSpace(type.Name) == true)
        {
            WriteLine($"Skipping {type.FullName} because it has no name.");

            returnValue = false;
        }

        if (MatchesNamespaceFilter(type, filterByNamespace) == false)
        {
            WriteLine($"Skipping {type.FullName} because it does not match namespace filter '{filterByNamespace}'.");
            returnValue = false;
        }

        if (filterByTypeNames.IsNullOrEmpty() == true)
        {
            // no filter
        }
        else if (typeNameExactMatch == true &&
            filterByTypeNames.Contains(type.Name.ToLower()) == false)
        {
            WriteLine($"Skipping {type.FullName} because it does not match type name filter '{string.Join(",", filterByTypeNames)}'.");

            returnValue = false;
        }
        else if (typeNameExactMatch == false &&
            filterByTypeNames.Any(x => type.Name.ToLower().Contains(x)) == false)
        {
            WriteLine($"Skipping {type.FullName} because it does not match type name filter '{string.Join(",", filterByTypeNames)}'.");

            returnValue = false;
        }

        if (returnValue == true)
        {
            // WriteLine($"Including {type.FullName}.");
        }
        else
        {
            WriteLine($"Excluding {type.FullName}.");
        }

        return returnValue;
    }

    private bool MatchesNamespaceFilter(Type type, string filterByNamespace)
    {
        if (string.IsNullOrWhiteSpace(filterByNamespace) == false)
        {
            string? namespaceName;

            try
            {
                namespaceName = type.Namespace;

                if (string.IsNullOrWhiteSpace(namespaceName) == true)
                {
                    return false;
                }
                else if (namespaceName.Contains(
                    filterByNamespace,
                    StringComparison.CurrentCultureIgnoreCase) == false)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    private void OpenFileInBrowser(string outputFilename)
    {
        if (OperatingSystem.IsMacOS())
        {
            System.Diagnostics.Process.Start("open", outputFilename);
        }
        else if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(outputFilename)
                {
                    UseShellExecute = true
                });
        }
        else if (OperatingSystem.IsLinux())
        {
            System.Diagnostics.Process.Start("xdg-open", outputFilename);
        }
        else
        {
            throw new InvalidOperationException("Operating system not supported.");
        }
    }


    private void WriteDiagramToFile(
        StringBuilder diagramBuilder, string outputFilename,
        string assemblyName, string filterByNamespace)
    {
        var htmlTemplate = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Class Diagram for %%FILENAME%%</title>
    <script type=""module"">
        // Import the Mermaid library from a CDN
        import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';

        // Initialize Mermaid on page load
        document.addEventListener(""DOMContentLoaded"", () => {
            mermaid.initialize({ startOnLoad: true });
        });
    </script>
</head>
<body>
    <h1>Class Diagram for %%FILENAME%%</h1>
    %%NAMESPACE_FILTER%%
    

    <!-- Mermaid Class Diagram Container -->
    <div class=""mermaid"">
classDiagram
%%DIAGRAM%%
    </div>
</body>
</html>";

        if (string.IsNullOrWhiteSpace(filterByNamespace) == false)
        {
            htmlTemplate = htmlTemplate.Replace("%%NAMESPACE_FILTER%%",
                $"<h2>Filtered by namespace: {filterByNamespace}</h2>");
        }
        else
        {
            htmlTemplate = htmlTemplate.Replace("%%NAMESPACE_FILTER%%",
                string.Empty);
        }

        htmlTemplate = htmlTemplate.Replace("%%DIAGRAM%%", diagramBuilder.ToString());
        htmlTemplate = htmlTemplate.Replace("%%FILENAME%%", assemblyName);

        System.IO.File.WriteAllText(outputFilename, htmlTemplate);

        WriteLine();
        WriteLine();

        WriteLine(htmlTemplate);

        WriteLine();
        WriteLine();

        WriteLine($"Wrote class diagram to:");
        WriteLine(outputFilename);
    }

    public const string ABSTRACT_TAG = @"&lt;&lt;abstract&gt;&gt;";
    public const string INTERFACE_TAG = @"&lt;&lt;interface&gt;&gt;";

    private void AddTypeToDiagram(StringBuilder document, Type type)
    {
        

        var properties = type.GetProperties();
        var methods = type.GetMethods();

        var className = GetName(type);

        if (className.StartsWith("<") == true)
        {
            WriteLine($"Skipping {type.FullName} because it starts with an unprintable character.");

            return;
        }
        else if (
            HasDisplayableMethodsAndProperties(properties, methods) == false)
        {
            WriteLine($"Skipping {type.FullName} because it has no displayable methods or properties.");

            return;
        }


        var builder = new StringBuilder();

        // I need to build the class diagram content before deciding what the 
        // class diagram starting syntax should be.  That's why i'm not writing 
        // this to the document yet.
        //
        // Deliberately not writing this line --> builder.AppendLine($"class {GetName(type)} {{");

        if (type.IsInterface == true)
        {
            builder.AppendLine($"    {INTERFACE_TAG}");
        }
        else if (type.IsClass == true && type.IsAbstract == true)
        {
            builder.AppendLine($"    {ABSTRACT_TAG}");
        }

        foreach (var property in properties)
        {
            builder.AppendLine($"    +{GetName(property.PropertyType)} {property.Name}");
        }

        foreach (var method in methods)
        {
            if (IsPropertyMethod(method) == true)
            {
                continue;
            }

            if (IsSystemObjectMethod(method) == true)
            {
                continue;
            }

            if (method.IsAbstract == true)
            {
                builder.AppendLine($"    +{method.Name}() {GetReturnTypeSafe(method)}*");
            }
            else
            {
                builder.AppendLine($"    +{method.Name}() {GetReturnTypeSafe(method)}");
            }
        }

        // add content to the diagram
        if (builder.Length == 0)
        {
            document.AppendLine($"class {GetName(type)}");
            document.AppendLine();
        }
        else
        {
            document.AppendLine($"class {GetName(type)} {{");
            document.AppendLine(builder.ToString());
            document.AppendLine("}");
        }
    }

    private bool HasDisplayableMethodsAndProperties(PropertyInfo[] properties, MethodInfo[] methods)
    {
        if (properties.Length == 0 && methods.Length == 0)
        {
            return false;
        }
        else if (properties.Length > 0)
        {
            return true;
        }
        else
        {
            bool hasDisplayable = false;

            foreach (var method in methods)
            {
                if (IsPropertyMethod(method) == true)
                {
                    continue;
                }

                if (IsSystemObjectMethod(method) == true)
                {
                    continue;
                }

                hasDisplayable = true;
                break;
            }

            return hasDisplayable;
        }
    }


    private string GetName(Type type)
    {
        if (type.IsGenericType == true)
        {
            var name = type.Name;

            if (name.StartsWith("<") == true)
            {
                var fullName = type.FullName ?? string.Empty;

                // get everything after the last period
                var lastPeriodIndex = fullName.LastIndexOf('.');

                if (lastPeriodIndex > 0)
                {
                    name = fullName.Substring(lastPeriodIndex + 1);
                }

                return name;
            }

            var backtickIndex = name.IndexOf('`');

            if (backtickIndex > 0)
            {
                name = name.Substring(0, backtickIndex);
            }

            var genericArguments = type.GetGenericArguments();

            var genericArgumentNames = new List<string>();

            foreach (var genericArgType in genericArguments)
            {
                if (genericArgType == null)
                {
                    genericArgumentNames.Add(string.Empty);
                }
                else if (genericArgType.IsGenericParameter == true)
                {
                    genericArgumentNames.Add(genericArgType.Name);
                }
                else
                {
                    genericArgumentNames.Add(GetName(genericArgType));
                }
            }

            return $"{name}~{string.Join(",", genericArgumentNames)}~";
        }
        else
        {
            return type.Name;
        }
    }


    private bool IsPropertyMethod(MethodInfo method)
    {
        // Check if the method is a getter or setter for a property
        return method.IsSpecialName &&
               (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"));
    }

    private bool IsSystemObjectMethod(MethodInfo method)
    {
        var ignore = new List<string>
        {
            "ToString",
            "Equals",
            "GetHashCode",
            "GetType"
        };

        return ignore.Contains(method.Name);
    }

    private string GetReturnTypeSafe(MethodInfo method)
    {
        try
        {
            return GetName(method.ReturnType);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
