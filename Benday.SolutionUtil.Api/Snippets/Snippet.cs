using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Benday.SolutionUtil.Api.Snippets;

public class Snippet
{
    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string[] Body { get; set; } = Array.Empty<string>();

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    private List<SnippetVariable>? _Variables;

    [JsonIgnore]
    public List<SnippetVariable> Variables
    {
        get
        {
            if (_Variables == null)
            {
                _Variables = GetVariables();
            }

            return _Variables;
        }
        set => _Variables = value;
    }

    private List<SnippetVariable> GetVariables()
    {
        var returnValues = new SortedList<string, SnippetVariable>();
        List<SnippetVariable> lineVariables;

        foreach (var line in Body)
        {
            lineVariables = GetVariables(line);

            foreach (var item in lineVariables)
            {
                if (item.IsNestedToken == false &&
                    returnValues.ContainsKey(item.Number) == true)
                {
                    // cleanup the variable name
                    // because nested variables aren't nameable
                    // when parsing them
                    if (returnValues[item.Number].IsNestedToken == true)
                    {
                        returnValues.Remove(item.Number);
                    }
                }

                if (returnValues.ContainsKey(item.Number) == false)
                {
                    returnValues.Add(item.Number, item);
                }
            }
        }

        return returnValues.Values.ToList();
    }


    private List<SnippetVariable> ParseVariablesForNestedTokens(string line)
    {
        var positions = StringParsingUtility.GetPositions(line);

        var theValue = StringParsingUtility.GetStringBetweenFirstTokens(line, positions);

        var variableName = StringParsingUtility.GetValueBeforeSlash(theValue);

        var variables = new List<SnippetVariable>();

        variables.Add(new SnippetVariable()
        {
            Number = variableName,
            Value = "Value",
            IsNestedToken = true
        });

        return variables;
    }

    private List<SnippetVariable> GetVariables(string line)
    {
        string startOfVar = "${";

        if (line == null || line.Contains(startOfVar) == false)
        {
            return new List<SnippetVariable>();
        }
        else
        {
            bool lineContainsNestedTokens = StringParsingUtility.ContainsNestedTokens(line);

            if (lineContainsNestedTokens)
            {
                return ParseVariablesForNestedTokens(line);
            }
            else
            {
                var firstTokenStart = line.IndexOf(startOfVar);

                var lineStartingWithToken = line.Substring(firstTokenStart);

                var tokens = lineStartingWithToken.Split(
                    new string[] { startOfVar }, StringSplitOptions.RemoveEmptyEntries);

                var returnValues = new List<SnippetVariable>();

                foreach (var token in tokens)
                {
                    var tempVariable = GetVariable(token);

                    returnValues.Add(tempVariable);
                }

                return returnValues;
            }
        }
    }

    private SnippetVariable GetVariable(string token)
    {
        var returnValue = new SnippetVariable();

        var closingPosition = token.IndexOf("}");
        int positionOfColon;

        if (closingPosition == -1)
        {
            throw new InvalidOperationException("Could not find closing.");
        }
        else
        {
            returnValue.Value = "${" + token.Substring(0, closingPosition + 1);

            returnValue.Number = token[0].ToString();

            if (token.Contains(":") == false)
            {
                if (returnValue.Number == "1")
                {
                    returnValue.Name = "Value";
                }
                else
                {
                    returnValue.Name = String.Format("Value{0}", returnValue.Number);
                }
            }
            else
            {
                positionOfColon = token.IndexOf(":");

                var builder = new StringBuilder();

                for (int i = positionOfColon + 1; i < token.Length - 1; i++)
                {
                    if (token[i] == '}')
                    {
                        break;
                    }

                    builder.Append(token[i]);
                }

                returnValue.Name = builder.ToString();
            }
        }

        return returnValue;
    }
}
