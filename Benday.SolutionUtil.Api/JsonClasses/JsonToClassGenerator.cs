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

                /*
                string typeName = string.Empty;

                try
                {
                    typeName = GetTypeName(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"");
                }

                if (typeName == "string" || typeName == "int" || typeName == "bool")
                {
                    // skip it
                }
                else
                {
                    PopulateFromJsonObject(item.AsObject(), className);
                }
                */
            }
        }
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
                toClass.AddProperty(
                    item.Key,
                    $"{Singularize(item.Key)}",
                    true
                );

                PopulateFromArray((JsonArray)item.Value, Singularize(item.Key));
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

            sb.AppendLine($"public class {item.Key}");
            sb.AppendLine("{");

            foreach (var prop in item.Value.Properties)
            {
                sb.AppendLine($"[JsonPropertyName(\"{prop.Value.Name}\")]");

                if (prop.Value.IsArray == true)
                {
                    sb.AppendLine($"    public {prop.Value.DataType}[] {prop.Value.Name} {{ get; set; }} = new {prop.Value.DataType}[0];");
                }
                else
                {
                    if (prop.Value.DataType == "string")
                    {
                        sb.AppendLine($"    public {prop.Value.DataType} {prop.Value.Name} {{ get; set; }} = string.Empty;");
                    }
                    else
                    {
                        sb.AppendLine($"    public {prop.Value.DataType} {prop.Value.Name} {{ get; set; }}");
                    }
                }
            }

            sb.AppendLine("}");

            GeneratedClasses.Add(item.Key, sb.ToString());
        }
    }

    public Dictionary<string, ClassInfo> Classes { get; set; } = new();
    public Dictionary<string, string> GeneratedClasses { get; set; } = new();

}
