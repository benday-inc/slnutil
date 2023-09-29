using System.Text.Json;
using System.Text.Json.Nodes;

namespace Benday.SolutionUtil.Api;

public class JsonToClassGenerator
{
    public JsonToClassGenerator()
    {
        
    }

    public void Parse(string json, string rootClass = "RootClass")
    {
        // deserialize json
        var result = JsonObject.Parse(json);

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
        //foreach (var item in array)
        //{
        //    
        //}

        AddClass(className);
    }

    private void PopulateFromJsonObject(JsonObject fromValue, string className)
    {
        AddClass(className);

        foreach (var item in fromValue)
        {
            Console.WriteLine($"{item}");
        }
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
