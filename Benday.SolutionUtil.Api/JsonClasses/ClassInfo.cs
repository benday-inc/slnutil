using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.SolutionUtil.Api.JsonClasses;
public class ClassInfo
{
    private const string DEFAULT_PROPERTY_TYPE = "string";

    public string Name { get; set; } = string.Empty;

    public Dictionary<string, PropertyInfo> Properties { get; set; } = new();

    public PropertyInfo AddProperty(string name)
    {
        return AddProperty(name, name, DEFAULT_PROPERTY_TYPE, false);
    }

    public PropertyInfo AddProperty(string jsonName, string name)
    {
        return AddProperty(jsonName, name, DEFAULT_PROPERTY_TYPE, false);
    }

    public PropertyInfo AddProperty(
        string jsonName,
        string name,
        string dataType)
    {
        return AddProperty(jsonName, name, dataType, false);
    }

     public PropertyInfo AddProperty(
        string jsonName,
        string name, 
        string dataType, 
        bool isArray)
    {
        if (Properties.ContainsKey(name) == true)
        {
            return Properties[name];
        }
        else
        {
            var prop = new PropertyInfo()
            {
                JsonName = jsonName,
                Name = name,
                DataType = dataType,
                IsArray = isArray
            };

            Properties.Add(name, prop);

            return prop;
        }
    }
}
