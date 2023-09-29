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
        AddClass(className);

        foreach (var item in fromValue)
        {
            Console.WriteLine($"item: {item}");

            if (item.Value is JsonObject)
            {
                PopulateFromJsonObject((JsonObject)item.Value, item.Key);
            }
            else if (item.Value is JsonArray)
            {
                PopulateFromArray((JsonArray)item.Value, Singularize(item.Key));
            }
        }
    }

    private string Singularize(string value)
    {
        IPluralize pluralizer = new Pluralizer();

        var returnValue = pluralizer.Singularize(value);

        return returnValue;
    }

    private void AddClass(string className)
    {
        if (Classes.Contains(className) == false)
        {
            Classes.Add(className);
        }
    }

    private List<string> _classes = new List<string>();
    public List<string> Classes
    {
        get => _classes;
        set => _classes = value;
    }

}
