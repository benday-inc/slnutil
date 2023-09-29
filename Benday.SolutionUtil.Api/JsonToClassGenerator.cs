using System.Text.Json;

namespace Benday.SolutionUtil.Api;

public class JsonToClassGenerator
{
    public JsonToClassGenerator()
    {
        
    }

    public void Parse(string json, string rootClass = "RootClass")
    {
        // deserialize json
        var result = JsonSerializer.Deserialize<dynamic>(json);

        if (result is null)
        {
            return;
        }
        else if (result is JsonElement)
        {
            var element = (JsonElement)result;

            PopulateFromElement(element, rootClass);
        }        
    }

    private void PopulateFromElement(JsonElement element, string className)
    {
        if (Classes.Contains(className) == false)
        {
            Classes.Add(className);
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in element.EnumerateObject())
            {
                Console.WriteLine($"{prop.Name}: {prop.Value}");
            }
        }
    }

    private List<string> _classes = new List<string>();
    public List<string> Classes
    {
        get => _classes;
        set => _classes = value;
    }
    
}
