using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Benday.SolutionUtil.Api;
public class JsonEditor
{
    private JObject _JsonFile;
    private string _PathToFile;

    public JsonEditor(string pathToFile)
    {
        if (string.IsNullOrEmpty(pathToFile))
            throw new ArgumentException($"{nameof(pathToFile)} is null or empty.", nameof(pathToFile));

        _PathToFile = pathToFile;

        AssertFileExists(_PathToFile);

        _JsonFile = LoadJsonFile(_PathToFile);
    }

    public string GetValue(params string[] nodes)
    {
        if (nodes == null || nodes.Length == 0)
            throw new ArgumentException(
                $"{nameof(nodes)} is null or empty.", nameof(nodes));
        var query = GetJsonQueryForNodes(nodes);

        return GetValueUsingQuery(query.ToString());
    }

    private string GetJsonQueryForNodes(params string[] nodes)
    {
        bool needsPeriod = false;

        var query = new StringBuilder();

        foreach (var node in nodes)
        {
            if (needsPeriod == true)
            {
                query.Append(".");
            }

            query.Append(node);

            needsPeriod = true;
        }

        return query.ToString();
    }

    private void CreateNodeStructure(string[] nodes)
    {
        if (nodes == null || nodes.Length == 0)
            throw new ArgumentException($"{nameof(nodes)} is null or empty.", nameof(nodes));

        JObject parent = null;

        for (int i = 0; i < nodes.Length; i++)
        {
            var current = GetJToken(_JsonFile,
                GetJsonQueryForNodes(nodes.Take(i + 1).ToArray()));

            if (current == null)
            {
                if ((nodes.Length - i) > 1)
                {
                    // node is somewhere in the middle of structure
                    JObject tempContainer = new JObject();
                    var temp = new JProperty(nodes[i], tempContainer);

                    if (parent == null)
                    {
                        _JsonFile.Add(temp);
                    }
                    else
                    {
                        parent.Add(temp);
                    }

                    parent = tempContainer;
                }
                else
                {
                    // end of node structure
                    var temp = new JProperty(nodes[i], String.Empty);

                    if (parent == null)
                    {
                        _JsonFile.Add(temp);
                    }
                    else
                    {
                        parent.Add(temp);
                    }
                }
            }
            else
            {
                if (current is not JObject)
                {
                    throw new InvalidOperationException($"There is already a property value with this name. Cannot change a property to an object.");
                }

                parent = (JObject)current;
            }
        }
    }

    public void SetValue(string nodeValue, params string[] nodes)
    {
        if (string.IsNullOrEmpty(nodeValue))
            throw new ArgumentException($"{nameof(nodeValue)} is null or empty.", nameof(nodeValue));
        if (nodes == null || nodes.Length == 0)
            throw new ArgumentException(
                $"{nameof(nodes)} is null or empty.", nameof(nodes));

        var query = GetJsonQueryForNodes(nodes);

        var match = GetJToken(_JsonFile, query);

        if (match != null)
        {
            match.Replace(new JValue(nodeValue));
        }
        else
        {
            CreateNodeStructure(nodes);
            SetValue(nodeValue, nodes);
        }

        WriteJsonFile();
    }

    private void SetValueUsingQuery(string query, string value)
    {
        var match = GetJToken(_JsonFile, query);

        if (match != null)
        {
            match.Replace(new JValue(value));
        }
        else
        {
            throw new InvalidOperationException("No matching node.");
        }

        WriteJsonFile();
    }

    private void WriteJsonFile()
    {
        File.WriteAllText(
        _PathToFile,
            JsonConvert.SerializeObject(_JsonFile, Newtonsoft.Json.Formatting.Indented));
    }

    private JToken GetJToken(JObject json, string query)
    {
        var match = json.SelectToken(query);

        return match;
    }

    private string GetValueUsingQuery(string query)
    {
        JToken match = GetJToken(
            _JsonFile, query);

        if (match == null)
        {
            return null;
        }
        else
        {
            return match.Value<string>();
        }
    }

    private JObject LoadJsonFile(string pathToFile)
    {
        AssertFileExists(pathToFile);

        var jsonText = File.ReadAllText(pathToFile);

        var json = JObject.Parse(jsonText);

        return json;
    }

    private void AssertFileExists(string pathToFile)
    {
        if (File.Exists(pathToFile) == false)
        {
            throw new FileNotFoundException("File not found.", pathToFile);
        }
    }

}

