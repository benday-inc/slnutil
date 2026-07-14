using System.ComponentModel;
using System.Text;

using Benday.SolutionUtil.Api.JsonClasses;

using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Benday.SolutionUtil.Api.Mcp.Tools;

/// <summary>
/// Read-only MCP tools that perform pure text transforms: base64 encoding,
/// JSON/XML formatting (preview only), JSON-to-C# class generation, and
/// character-by-character file printing for encoding diagnosis.
/// </summary>
[McpServerToolType]
public class TransformTools
{
    [McpServerTool(Name = "base64_encode", ReadOnly = true, Idempotent = true),
        Description(
            "Encode a string value as base64, matching slnutil's 'base64' command " +
            "(prefixes the value with a colon before encoding, as used for HTTP basic " +
            "auth headers). Returns the encoded string.")]
    public static string Base64Encode(
        [Description("The string value to encode as base64.")]
        string value)
    {
        return CommandToolRunner.Run<ToBase64Command>(new Dictionary<string, string?>
        {
            [Constants.ArgumentNameValue] = value
        });
    }

    [McpServerTool(Name = "format_json", ReadOnly = true),
        Description(
            "Format (pretty-print) a JSON file and return the formatted text. This is a " +
            "preview only: it never writes changes back to the file. Requires the absolute " +
            "path to a single .json file.")]
    public static string FormatJson(
        [Description("Absolute path to the .json file to format. Wildcards are not supported.")]
        string filePath)
    {
        return CommandToolRunner.Run<FormatJsonCommand>(new Dictionary<string, string?>
        {
            [Constants.ArgumentNameFilename] = filePath
        });
    }

    [McpServerTool(Name = "format_xml", ReadOnly = true),
        Description(
            "Format (pretty-print) an XML file and return the formatted text. This is a " +
            "preview only: it never writes changes back to the file. Requires the absolute " +
            "path to a single .xml file.")]
    public static string FormatXml(
        [Description("Absolute path to the .xml file to format. Wildcards are not supported.")]
        string filePath)
    {
        return CommandToolRunner.Run<FormatXmlCommand>(new Dictionary<string, string?>
        {
            [Constants.ArgumentNameFilename] = filePath
        });
    }

    [McpServerTool(Name = "print_file", ReadOnly = true),
        Description(
            "Read a text file and return its contents followed by a character-by-character " +
            "breakdown (each character with its ASCII/Unicode code point). Use this to " +
            "diagnose encoding problems, invisible characters, or unexpected whitespace. " +
            "Requires the absolute path to the file.")]
    public static string PrintFile(
        [Description("Absolute path to the text file to read and analyze.")]
        string filePath)
    {
        return CommandToolRunner.Run<PrintFileCommand>(new Dictionary<string, string?>
        {
            ["input"] = filePath
        });
    }

    [McpServerTool(Name = "classes_from_json", ReadOnly = true),
        Description(
            "Generate C# classes from a JSON document, with System.Text.Json serialization " +
            "attributes. Pass the JSON as a string; returns the generated C# source. Use " +
            "this to turn a sample API response or config payload into strongly-typed " +
            "classes.")]
    public static string ClassesFromJson(
        [Description("The JSON document to convert into C# classes.")]
        string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new McpException("Input does not contain any JSON text.");
        }

        var generator = new JsonToClassGenerator
        {
            InnerClassMode = false,
            RootClassName = "RootClass"
        };

        try
        {
            generator.Parse(json);
            generator.GenerateClasses();
        }
        catch (Exception ex)
        {
            throw new McpException($"Could not parse JSON: {ex.Message}");
        }

        if (generator.GeneratedClasses.Count == 0)
        {
            throw new McpException("Input did not produce any classes.");
        }

        var code = new StringBuilder();

        foreach (var key in generator.GeneratedClasses.Keys)
        {
            code.AppendLine(generator.GeneratedClasses[key]);
            code.AppendLine();
        }

        return code.ToString();
    }
}
