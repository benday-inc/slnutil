using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Pluralize.NET;

namespace Benday.SolutionUtil.Api.JsonClasses;

public class JsonToClassGenerator
{
    public JsonToClassGenerator()
    {

    }

    public void Parse(string json, string rootClass = "RootClass")
    {
        var options = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip, // or JsonCommentHandling.Allow
            AllowTrailingCommas = true // optional: also allow trailing commas
        };

        // deserialize json with comment support
        var result = JsonNode.Parse(json, null, options);

        if (result is null)
        {
            return;
        }
        else if (result is JsonArray)
        {
            var array = (JsonArray)result;

            PopulateFromArray(array, rootClass);
        }
        else if (result is JsonObject)
        {
            var element = (JsonObject)result;

            PopulateFromJsonObject(element, rootClass);
        }
    }
    private void PopulateFromArray(JsonArray array, string className)
    {
        foreach (var item in array)
        {
            if (item is null)
            {
                continue;
            }
            else
            {
                if (item is JsonObject)
                {
                    PopulateFromJsonObject(item.AsObject(), className);
                }
            }
        }
    }

    
    private ArrayDataTypeInfo GetArrayElementDataType(JsonArray itemValueAsArray)
    {
        var returnValue = new ArrayDataTypeInfo();

        returnValue.ProposedDataType = "unknown";
        returnValue.IsEmpty = true;

        JsonElement asElement;

        foreach (var item in itemValueAsArray)
        {
            if (item is null)
            {
                continue;
            }

            if (item is JsonObject)
            {
                returnValue.ProposedDataType = "unknown";
                returnValue.IsScalar = false;
                returnValue.IsEmpty = false;

                return returnValue;
            }
            else
            {
                asElement = item.GetValue<JsonElement>();

                returnValue.Kind = asElement.ValueKind;
                returnValue.IsEmpty = false;

                break;
            }
        }

        if (returnValue.Kind == JsonValueKind.String)
        {
            returnValue.ProposedDataType = "string";
            returnValue.IsScalar = true;
        }
        else if (returnValue.Kind == JsonValueKind.Number)
        {
            returnValue.ProposedDataType = "int";
            returnValue.IsScalar = true;
        }
        else if (returnValue.Kind == JsonValueKind.True)
        {
            returnValue.ProposedDataType = "bool";
            returnValue.IsScalar = true;
        }
        else if (returnValue.Kind == JsonValueKind.False)
        {
            returnValue.ProposedDataType = "bool";
            returnValue.IsScalar = true;
        }
        else if (returnValue.Kind == JsonValueKind.Object)
        {
            returnValue.ProposedDataType = "unknown";
            returnValue.IsScalar = false;
        }

        return returnValue;
    }
    private void PopulateFromJsonObject(JsonObject fromValue, string className)
    {
        var toClass = AddClass(className);

        foreach (var item in fromValue)
        {
            var formattedKey = Utilities.JsonNameToCsharpName(item.Key);
            var originalKey = item.Key;

            if (item.Value is JsonObject)
            {
                toClass.AddProperty(
                    originalKey,
                    formattedKey,
                    formattedKey
                );

                PopulateFromJsonObject((JsonObject)item.Value, formattedKey);
            }
            else if (item.Value is JsonArray)
            {
                var arrayElementDataType = GetArrayElementDataType((JsonArray)item.Value);

                if (arrayElementDataType.IsEmpty == true)
                {
                    // skip it...can't determine anything
                }
                else if (arrayElementDataType.IsScalar == true)
                {
                    toClass.AddProperty(
                        originalKey,
                        formattedKey,
                        $"{arrayElementDataType.ProposedDataType}",
                        true
                    );

                }
                else
                {
                    toClass.AddProperty(
                        originalKey,
                        formattedKey,
                        $"{Singularize(formattedKey)}",
                        true
                    );

                    PopulateFromArray((JsonArray)item.Value, Singularize(formattedKey));
                }
            }
            else
            {
                if (item.Value is null)
                {
                    toClass.AddProperty(originalKey, formattedKey);
                }
                else
                {
                    toClass.AddProperty(originalKey, formattedKey, GetTypeName(item));
                }
            }
        }
    }

    private string GetTypeName(KeyValuePair<string, JsonNode?> item)
    {
        if (item.Value is null)
        {
            return "string";
        }
        else
        {
            var temp = item.Value;
            return GetTypeName(temp);
        }
    }

    private static string GetTypeName(JsonNode temp)
    {
        var element = temp.GetValue<JsonElement>();

        if (element.ValueKind == JsonValueKind.String)
        {
            // check if the string is a valid date time
            if (DateTime.TryParse(element.GetString(), out _))
            {
                return "DateTime";
            }

            return "string";
        }
        else if (element.ValueKind == JsonValueKind.Number)
        {
            return "int";
        }
        else if (element.ValueKind == JsonValueKind.True ||
            element.ValueKind == JsonValueKind.False)
        {
            return "bool";
        }
        else
        {
            return element.ValueKind.ToString();
        }
    }

    private string Singularize(string value)
    {
        IPluralize pluralizer = new Pluralizer();

        var returnValue = pluralizer.Singularize(value);

        return returnValue;
    }

    private ClassInfo AddClass(string className)
    {
        if (Classes.ContainsKey(className) == false)
        {
            var temp = new ClassInfo()
            {
                Name = className
            };

            Classes.Add(className, temp);

            return temp;
        }
        else
        {
            return Classes[className];
        }
    }

    public void GenerateClasses()
    {
        foreach (var item in Classes)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"public class {item.Key.Capitalize()}");
            sb.AppendLine("{");

            foreach (var prop in item.Value.Properties)
            {
                var propValueName = CleanPropertyValueName(prop);



                sb.AppendLine($"    [JsonPropertyName(\"{prop.Value.JsonName}\")]");

                if (prop.Value.IsArray == true && (prop.Value.DataType == "string" || prop.Value.DataType == "int"))
                {
                    sb.AppendLine($"    public {prop.Value.DataType}[] {propValueName.Capitalize()} {{ get; set; }} = new {prop.Value.DataType}[0];");
                }
                else if (prop.Value.IsArray == true)
                {
                    sb.AppendLine($"    public {prop.Value.DataType.Capitalize()}[] {propValueName.Capitalize()} {{ get; set; }} = new {prop.Value.DataType.Capitalize()}[0];");
                }
                else
                {
                    if (prop.Value.DataType == "string")
                    {
                        sb.AppendLine($"    public {prop.Value.DataType} {propValueName.Capitalize()} {{ get; set; }} = string.Empty;");
                    }
                    else if (prop.Value.DataType == "int" || prop.Value.DataType == "bool" || prop.Value.DataType == "DateTime")
                    {
                        sb.AppendLine($"    public {prop.Value.DataType} {propValueName.Capitalize()} {{ get; set; }}");
                    }
                    else
                    {
                        sb.AppendLine($"    public {prop.Value.DataType.Capitalize()} {propValueName.Capitalize()} {{ get; set; }} = new();");
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine("}");

            GeneratedClasses.Add(item.Key, sb.ToString());
        }
    }

    private string CleanPropertyValueName(KeyValuePair<string, PropertyInfo> prop)
    {
        var name = prop.Value.Name;
   
        var cleanedName = name;

        var isNumber = int.TryParse(name, out var parsedNumber);

        if (isNumber == true && parsedNumber >= 0)
        {
            cleanedName = "Val_" + parsedNumber;
        }
        else if (isNumber == true && parsedNumber < 0)
        {
            cleanedName = "Val_Minus_" + Math.Abs(parsedNumber).ToString();
        }
        
        if (char.IsDigit(cleanedName[0]))
        {
            cleanedName = "Val_" + cleanedName;
        }

        if (IsCSharpReservedWord(cleanedName))
        {
            cleanedName = "Val_" + cleanedName;
        }

        cleanedName = Utilities.RemoveCharToPascalCase('-', cleanedName);

        return cleanedName;
    }

    private bool IsCSharpReservedWord(string word)
    {
        var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        return reservedWords.Contains(word);
    }

    public Dictionary<string, ClassInfo> Classes { get; set; } = new();
    public Dictionary<string, string> GeneratedClasses { get; set; } = new();

    

}
