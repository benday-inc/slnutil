using System.Reflection;
using System.Text;

using Benday.CommandsFramework;
namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameClassDiagram,
    Description = "Replace token in file.")]
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
        var filename = Arguments.GetStringValue(Constants.ArgumentNameFilename);
        Utilities.AssertFileExists(filename, Constants.ArgumentNameFilename);

        var filterByNamespace = Arguments.GetStringValue(Constants.ArgumentsFilterByNamespace);

        var assembly = Assembly.LoadFile(filename);

        var types = GetTypesSafe(assembly);

        var builder = new StringBuilder();

        foreach (var type in types)
        {
            if (MatchesFilter(type, filterByNamespace) == false)
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

        WriteDiagramToFile(builder, outputFilename);

        OpenFileInBrowser(outputFilename);
    }

    private bool MatchesFilter(Type type, string filterByNamespace)
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
        Console.WriteLine($"Opening '{outputFilename}' in browser...");

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

        Console.WriteLine($"Opened '{outputFilename}' in browser.");
    }


    private void WriteDiagramToFile(StringBuilder diagramBuilder, string outputFilename)
    {
        var html = new StringBuilder();

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

    <!-- Mermaid Class Diagram Container -->
    <div class=""mermaid"">
classDiagram
%%DIAGRAM%%
    </div>
</body>
</html>";

        html.Append(htmlTemplate.Replace("%%DIAGRAM%%", diagramBuilder.ToString())
            .Replace("%%FILENAME%%", outputFilename));

        System.IO.File.WriteAllText(outputFilename, html.ToString());

        Console.WriteLine($"Wrote class diagram to");
        Console.WriteLine(outputFilename);
    }

    public const string ABSTRACT_TAG = @"&lt;&lt;abstract&gt;&gt;";
    public const string INTERFACE_TAG = @"&lt;&lt;interface&gt;&gt;";

    private void AddTypeToDiagram(StringBuilder builder, Type type)
    {
        builder.AppendLine($"class {type.Name} {{");

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
            builder.AppendLine($"    +{property.PropertyType.Name} {property.Name}");
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

        System.Console.WriteLine($"{builder.ToString()}");
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
            return method.ReturnType.Name;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
