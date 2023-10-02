using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Pluralize.NET;
using System.Drawing;

namespace Benday.SolutionUtil.Api.JsonClasses;

public class JsonToClassGenerator
{
    public JsonToClassGenerator()
    {

    }

    public void Parse(string json, string rootClass = "RootClass")
    {
        // deserialize json
        var result = JsonNode.Parse(json);

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

    private class ArrayDataTypeInfo
    {
        public bool IsScalar { get; set; }
        public JsonValueKind Kind { get; set; }
        public string ProposedDataType { get; set; } = string.Empty;
        public bool IsEmpty { get; set; }
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
            if (item.Value is JsonObject)
            {
                toClass.AddProperty(
                    item.Key,
                    item.Key
                );

                PopulateFromJsonObject((JsonObject)item.Value, item.Key);
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
                        item.Key,
                        $"{arrayElementDataType.ProposedDataType}",
                        true
                    );

                }
                else
                {
                    toClass.AddProperty(
                        item.Key,
                        $"{Singularize(item.Key)}",
                        true
                    );

                    PopulateFromArray((JsonArray)item.Value, Singularize(item.Key));
                }
            }
            else
            {
                if (item.Value is null)
                {
                    toClass.AddProperty(item.Key);
                }
                else
                {
                    toClass.AddProperty(item.Key, GetTypeName(item));
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
                sb.AppendLine($"    [JsonPropertyName(\"{prop.Value.Name}\")]");

                if (prop.Value.IsArray == true && (prop.Value.DataType == "string" || prop.Value.DataType == "int"))
                {
                    sb.AppendLine($"    public {prop.Value.DataType}[] {prop.Value.Name.Capitalize()} {{ get; set; }} = new {prop.Value.DataType}[0];");
                }
                else if (prop.Value.IsArray == true)
                {
                    sb.AppendLine($"    public {prop.Value.DataType.Capitalize()}[] {prop.Value.Name.Capitalize()} {{ get; set; }} = new {prop.Value.DataType.Capitalize()}[0];");
                }
                else
                {
                    if (prop.Value.DataType == "string")
                    {
                        sb.AppendLine($"    public {prop.Value.DataType} {prop.Value.Name.Capitalize()} {{ get; set; }} = string.Empty;");
                    }
                    else if (prop.Value.DataType == "int" || prop.Value.DataType == "bool")
                    {
                        sb.AppendLine($"    public {prop.Value.DataType} {prop.Value.Name.Capitalize()} {{ get; set; }}");
                    }
                    else
                    {
                        sb.AppendLine($"    public {prop.Value.DataType.Capitalize()} {prop.Value.Name.Capitalize()} {{ get; set; }} = new();");
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine("}");

            GeneratedClasses.Add(item.Key, sb.ToString());
        }
    }

    public Dictionary<string, ClassInfo> Classes { get; set; } = new();
    public Dictionary<string, string> GeneratedClasses { get; set; } = new();

}
