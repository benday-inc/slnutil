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
            return assembly.GetTypes();
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
                if (MatchesFilter(type, filterByNamespace, filterByTypeNames, typeNameExactMatch) == false)
                {
                    continue;
                }

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
        if (type == null)
        {
            return false;
        }

        if (type.IsNestedPrivate == true)
        {
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(type.Name) == true)
        {
            return false;
        }

        if (MatchesNamespaceFilter(type, filterByNamespace) == false)
        {
            return false;
        }

        if (filterByTypeNames.IsNullOrEmpty() == true)
        {
            return true;
        }
        else if (typeNameExactMatch == true &&
            filterByTypeNames.Contains(type.Name.ToLower()) == false)
        {
            return false;
        }
        else if (typeNameExactMatch == false &&
            filterByTypeNames.Any(x => type.Name.ToLower().Contains(x)) == false)
        {
            return false;
        }
        else
        {
            return true;
        }
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
            System.Diagnostics.Process.Start(outputFilename);
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

    private void AddTypeToDiagram(StringBuilder builder, Type type)
    {
        builder.AppendLine($"class {GetName(type)} {{");

        if (type.IsInterface == true)
        {
            builder.AppendLine($"    {INTERFACE_TAG}");
        }
        else if (type.IsClass == true && type.IsAbstract == true)
        {
            builder.AppendLine($"    {ABSTRACT_TAG}");
        }

        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            builder.AppendLine($"    +{GetName(property.PropertyType)} {property.Name}");
        }

        var methods = type.GetMethods();

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

        builder.AppendLine("}");
    }

    private string GetName(Type type)
    {
        if (type.IsGenericType == true)
        {
            var name = type.Name;

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
