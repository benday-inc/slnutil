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
                PopulateFromJsonObject(item.AsObject(), className);
            }
        }
    }

    private void PopulateFromJsonObject(JsonObject fromValue, string className)
    {
        var toClass = AddClass(className);

        foreach (var item in fromValue)
        {
            Console.WriteLine($"item: {item}");

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
                    $"List<{Singularize(item.Key)}>"
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
                    var temp = item.Value.GetValue<object>();

                    toClass.AddProperty(item.Key, temp.GetType().Name);
                }

            }
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

    private Dictionary<string, ClassInfo> _classes = new();
    public Dictionary<string, ClassInfo> Classes
    {
        get => _classes;
        set => _classes = value;
    }

}
