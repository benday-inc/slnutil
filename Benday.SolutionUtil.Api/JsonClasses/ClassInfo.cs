using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.SolutionUtil.Api.JsonClasses;
public class ClassInfo
{
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, PropertyInfo> Properties { get; set; } = new();

    public PropertyInfo AddProperty(string name, 
        string dataType = "string", 
        bool isArray = false)
    {
        if (Properties.ContainsKey(name) == true)
        {
            return Properties[name];
        }
        else
        {
            var prop = new PropertyInfo()
            {
                Name = name,
                DataType = dataType,
                IsArray = isArray
            };

            Properties.Add(name, prop);

            return prop;
        }
    }
}
