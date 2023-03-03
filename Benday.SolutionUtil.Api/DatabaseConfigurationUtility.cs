using System.Xml.Linq;
namespace Benday.SolutionUtil.Api;

public class DatabaseConfigurationUtility
{
    public static string GetConnectionString(string filename, string connectionName)
    {
        if (String.IsNullOrEmpty(filename))
            throw new ArgumentException("filename is null or empty.", "filename");
        if (String.IsNullOrEmpty(connectionName))
            throw new ArgumentException("connectionName is null or empty.", "connectionName");

        if (File.Exists(filename) == false)
        {
            throw new FileNotFoundException("Could not find the file.", filename);
        }

        var element = GetConnectionStringElement(filename, connectionName);

        if (element == null)
        {
            return null;
        }
        else if (element.Attribute("connectionString") == null)
        {
            return null;
        }
        else
        {
            return element.Attribute("connectionString").Value;
        }
    }

    private static void CreateElement(XDocument document, string connectionName, string updatedValue)
    {
        XElement element = new XElement("add");

        element.SetAttributeValue("name", connectionName);
        element.SetAttributeValue("connectionString", updatedValue);
        element.SetAttributeValue("providerName", "System.Data.SqlClient");

        var connectionStrings = document.Root.Element("connectionStrings");

        if (connectionStrings == null)
        {
            connectionStrings = new XElement("connectionStrings");
            document.Root.Add(connectionStrings);
        }

        connectionStrings.Add(element);
    }

    public static void SetConnectionString(string filename, string connectionName, string updatedValue)
    {
        if (String.IsNullOrEmpty(filename))
            throw new ArgumentException("filename is null or empty.", "filename");
        if (String.IsNullOrEmpty(connectionName))
            throw new ArgumentException("connectionName is null or empty.", "connectionName");
        if (updatedValue == null)
        {
            throw new ArgumentNullException("updatedValue", "Argument cannot be null.");
        }

        if (File.Exists(filename) == false)
        {
            throw new FileNotFoundException("Could not find the file.", filename);
        }

        File.SetAttributes(filename, FileAttributes.Normal);

        XDocument document = XDocument.Load(filename);

        var element = GetConnectionStringElement(document, filename, connectionName);

        if (element == null)
        {
            CreateElement(document, connectionName, updatedValue);

            Save(document, filename);
        }
        else
        {
            element.Attribute("connectionString").Value = updatedValue;

            Save(document, filename);
        }
    }
    static void Save(XDocument document, string filename)
    {
        File.WriteAllText(filename, document.ToString());
    }

    private static XElement GetConnectionStringElement(XDocument document, string filename, string connectionName)
    {
        var root = document.Root;

        if (root == null)
        {
            throw new InvalidOperationException("Document was empty.");
        }
        else if (root.Name != "configuration")
        {
            throw new InvalidOperationException("Document root node was not 'configuration'.");
        }
        else
        {
            var connectionStrings = root.Element("connectionStrings");

            if (connectionStrings == null)
            {
                return null;
            }
            else
            {
                var match = (from temp in connectionStrings.Elements("add")
                             where
                             temp.Name == "add" &&
                             temp.HasAttributes == true &&
                             temp.Attribute("name") != null &&
                             temp.Attribute("name").Value == connectionName
                             select temp).FirstOrDefault();

                return match;
            }
        }
    }

    private static XElement GetConnectionStringElement(string filename, string connectionName)
    {
        var document = XDocument.Load(filename);

        return GetConnectionStringElement(document, filename, connectionName);
    }
}