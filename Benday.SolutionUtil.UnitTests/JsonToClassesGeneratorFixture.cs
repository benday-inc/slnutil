using Benday.SolutionUtil.Api.JsonClasses;
using Benday.SolutionUtil.UnitTests.SampleClassesForSerialization;

namespace Benday.SolutionUtil.UnitTests;

[TestClass]
public partial class JsonToClassesGeneratorFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private JsonToClassGenerator? _SystemUnderTest;

    private JsonToClassGenerator SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new JsonToClassGenerator();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void GenerateClasses_ArrayOfScalars()
    {
        // arrange
        string json = "{ \"audiences\": [\"asdf\", \"qwer\", \"zxcv\"] }";

        SystemUnderTest.Parse(json);

        // act
        SystemUnderTest.GenerateClasses();

        // assert
        foreach (var item in SystemUnderTest.GeneratedClasses.Keys)
        {
            Console.WriteLine($"{item}");
        }

        Console.WriteLine($"** CLASSES **");

        Console.WriteLine($"{SystemUnderTest.GeneratedClasses.Values.FirstOrDefault()}");
    }


    [TestMethod]
    public void Serialize_Simple()
    {
        // arrange
        string actual = GetSimpleClassAsJson();

        // assert
        Console.WriteLine(actual);
    }

    private static string GetSimpleClassAsJson()
    {
        var item = new Thingy()
        {
            FavoriteNumber = 123,
            IsAwesome = true,
            Title = "asdf"
        };

        // act
        var actual = System.Text.Json.JsonSerializer.Serialize(item);
        return actual;
    }

    [TestMethod]
    public void Serialize_Simple_Array()
    {
        string actual = GetSimpleClassArrayAsJson();

        // assert
        Console.WriteLine(actual);
    }

    private static string GetSimpleClassArrayAsJson()
    {
        // arrange
        var items = new List<Thingy>
        {
            new Thingy()
            {
                FavoriteNumber = 123,
                IsAwesome = true,
                Title = "asdf"
            },
            new Thingy()
            {
                FavoriteNumber = 234,
                IsAwesome = false,
                Title = "qwer"
            }
        };


        // act
        var actual = System.Text.Json.JsonSerializer.Serialize(items);
        return actual;
    }

    [TestMethod]
    public void Serialize_Complex()
    {
        string actual = GetComplexClassAsJson();

        // assert
        Console.WriteLine(actual);
    }

    private static string GetComplexClassAsJson()
    {
        // arrange
        var item = new Person(1);

        item.AddAddress(1);
        item.AddAddress(2);

        // act
        var actual = System.Text.Json.JsonSerializer.Serialize(item);
        return actual;
    }

    [TestMethod]
    public void Serialize_Complex_Array()
    {
        string actual = GetComplexClassArrayAsJson();

        // assert
        Console.WriteLine(actual);
    }

    private static string GetComplexClassArrayAsJson()
    {
        // arrange
        var items = new List<Person>
        {
            new Person(1),
            new Person(2)
        };

        items[0].AddAddress(1);
        items[0].AddAddress(2);
        items[1].AddAddress(1);
        items[1].AddAddress(2);

        // act
        var actual = System.Text.Json.JsonSerializer.Serialize(items);
        return actual;
    }

    [TestMethod]
    public void ParseSimple_Single()
    {
        // arrange
        var fromJson = GetSimpleClassAsJson();

        var expectedClassCount = 1;
        var expectedClassName = "ThingyThing";
        var expectedClassNames = new List<string>()
        {
            expectedClassName
        };

        SystemUnderTest.RootClassName = expectedClassName;

        // act
        SystemUnderTest.Parse(fromJson);

        // assert
        Assert.AreEqual<int>(expectedClassCount, SystemUnderTest.Classes.Count, $"Class count is wrong");

        expectedClassNames.ForEach(name =>
            Assert.IsTrue(SystemUnderTest.Classes.Keys.Contains(name), $"Class name '{name}' not found"));

        AssertAreEqual(GetClassInfoForThingy(),
            SystemUnderTest.Classes["ThingyThing"]);
    }

    [TestMethod]
    public void ParseSimple_Single_ComplexClassName()
    {
        // arrange
        var fromJson = @"{
""fine_tuning"": {
        ""is_allowed_to_fine_tune"": false,
        ""finetuning_state"": ""not_started"",
        ""verification_attempts_count"": 0        
      }
}";

        var expectedClassInfo = new ClassInfo();

        expectedClassInfo.Name = "FineTuning";

        expectedClassInfo.AddProperty("is_allowed_to_fine_tune", "IsAllowedToFineTune", "bool");
        expectedClassInfo.AddProperty("finetuning_state", "FinetuningState");
        expectedClassInfo.AddProperty("verification_attempts_count", "VerificationAttemptsCount", "int");               

        var expectedClassCount = 2;
        var expectedClassNames = new List<string>()
        {
            "RootClass",
            "FineTuning"
        };

        // act
        SystemUnderTest.Parse(fromJson);

        // assert
        Assert.AreEqual<int>(expectedClassCount, SystemUnderTest.Classes.Count, $"Class count is wrong");

        expectedClassNames.ForEach(name =>
            Assert.IsTrue(SystemUnderTest.Classes.Keys.Contains(name), $"Class name '{name}' not found"));

        AssertAreEqual(expectedClassInfo,
            SystemUnderTest.Classes["FineTuning"]);
    }


    private void AssertAreEqual(ClassInfo expected,
        ClassInfo actual)
    {
        Assert.AreEqual<string>(expected.Name, actual.Name, "Name");

        Assert.AreEqual<int>(
            expected.Properties.Count,
            actual.Properties.Count,
            $"Properties.Count on '{expected.Name}' is wrong.");

        AssertAreEqual(expected.Properties, actual.Properties, expected.Name);
    }

    private void AssertAreEqual(
        Dictionary<string, PropertyInfo> expected,
        Dictionary<string, PropertyInfo> actual,
        string className)
    {
        var expectedKeys = expected.Keys;
        var actualKeys = actual.Keys;

        CollectionAssert.AreEqual(expectedKeys, actualKeys,
            $"Property key collections don't match on {className}.");

        foreach (var key in expectedKeys)
        {
            AssertAreEqual(expected[key], actual[key]);
        }

    }
    private void AssertAreEqual(
        PropertyInfo expected,
        PropertyInfo actual)
    {
        Assert.AreEqual<string>(expected.Name, actual.Name, "Name");
        Assert.AreEqual<string>(expected.DataType, actual.DataType, "DataType");
        Assert.AreEqual<bool>(expected.IsArray, actual.IsArray, "IsArray");
    }

    [TestMethod]
    public void ParseSimple_Array()
    {
        // arrange
        var fromJson = GetSimpleClassArrayAsJson();

        var expectedClassCount = 1;
        var expectedClassNames = new List<string>()
        {
            "RootClass"
        };

        // act
        SystemUnderTest.Parse(fromJson);

        // assert
        Assert.AreEqual<int>(expectedClassCount, SystemUnderTest.Classes.Count, $"Class count is wrong");

        expectedClassNames.ForEach(name =>
            Assert.IsTrue(SystemUnderTest.Classes.Keys.Contains(name), $"Class name '{name}' not found"));

        AssertAreEqual(GetClassInfoForThingy("RootClass"), SystemUnderTest.Classes["RootClass"]);
    }

    [TestMethod]
    public void GenerateClasses_Simple()
    {
        // arrange
        var fromJson = GetSimpleClassArrayAsJson();

        var expectedClassCount = 1;
        var expectedClassNames = new List<string>()
        {
            "RootClass"
        };

        SystemUnderTest.Parse(fromJson);

        Assert.AreEqual<int>(0, SystemUnderTest.GeneratedClasses.Count, "GeneratedClasses should be empty.");

        // act
        SystemUnderTest.GenerateClasses();

        // assert
        Assert.AreEqual<int>(expectedClassCount,
            SystemUnderTest.GeneratedClasses.Count,
            $"Generated class count is wrong");

        var actual = SystemUnderTest.GeneratedClasses["RootClass"];

        var expected =
        @"public class RootClass
{
    [JsonPropertyName(""Title"")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName(""IsAwesome"")]
    public bool IsAwesome { get; set; }

    [JsonPropertyName(""FavoriteNumber"")]
    public int FavoriteNumber { get; set; }
}";
        AssertCodeIsPrettyMuchEqual(expected, actual);
    }


    [TestMethod]
    public void GenerateClasses_Simple_ComplexNames()
    {
        // arrange
        var fromJson = @"{
""fine_tuning"": {
        ""is_allowed_to_fine_tune"": false,
        ""finetuning_state"": ""not_started"",
        ""verification_attempts_count"": 0,
        ""asdf-thingy-time"": 0
      }
}";

        var expectedClassCount = 2;
        var expectedClassNames = new List<string>()
        {
            "RootClass",
            "FineTuning"
        };

        SystemUnderTest.Parse(fromJson);

        Assert.AreEqual<int>(0, SystemUnderTest.GeneratedClasses.Count, "GeneratedClasses should be empty.");

        // act
        SystemUnderTest.GenerateClasses();

        // assert
        Assert.AreEqual<int>(expectedClassCount,
            SystemUnderTest.GeneratedClasses.Count,
            $"Generated class count is wrong");

        var actual = SystemUnderTest.GeneratedClasses["FineTuning"];

        var expected =
        @"public class FineTuning
{
    [JsonPropertyName(""is_allowed_to_fine_tune"")]
    public bool IsAllowedToFineTune { get; set; }

    [JsonPropertyName(""finetuning_state"")]
    public string FinetuningState { get; set; } = string.Empty;

    [JsonPropertyName(""verification_attempts_count"")]
    public int VerificationAttemptsCount { get; set; }

    [JsonPropertyName(""asdf-thingy-time"")]
    public int AsdfThingyTime { get; set; }
}";
        AssertCodeIsPrettyMuchEqual(expected, actual);
    }

    private void AssertCodeIsPrettyMuchEqual(string expected, string actual)
    {
        var expectedLines = expected.Trim().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var actualLines = actual.Trim().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.AreEqual<int>(expectedLines.Length, actualLines.Length, "Line count is wrong.");

        for (int i = 0; i < expectedLines.Length; i++)
        {
            Assert.AreEqual<string>(expectedLines[i].Trim(), actualLines[i].Trim(), $"Line {i} is wrong.");
        }
    }

    [TestMethod]
    public void GenerateClasses_Complex()
    {
        // arrange
        var fromJson = GetComplexClassArrayAsJson();

        var expectedClassCount = 3;
        var expectedClassNames = new List<string>()
        {
            "Person",
            "Address",
            "ThingyThing"
        };

        SystemUnderTest.RootClassName = "Person";

        SystemUnderTest.Parse(fromJson);

        Assert.AreEqual<int>(0, SystemUnderTest.GeneratedClasses.Count, "GeneratedClasses should be empty.");

        // act
        SystemUnderTest.GenerateClasses();

        // assert
        Assert.AreEqual<int>(expectedClassCount,
            SystemUnderTest.GeneratedClasses.Count,
            $"Generated class count is wrong");

        var actual = SystemUnderTest.GeneratedClasses["Person"];

        var expected = @"public class Person
{
    [JsonPropertyName(""Id"")]
    public int Id { get; set; }

    [JsonPropertyName(""FirstName"")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName(""LastName"")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName(""IsAwesome"")]
    public bool IsAwesome { get; set; }

    [JsonPropertyName(""FavoriteNumber"")]
    public int FavoriteNumber { get; set; }

    [JsonPropertyName(""Addresses"")]
    public Address[] Addresses { get; set; } = new Address[0];
}";

        AssertCodeIsPrettyMuchEqual(expected, actual);

        actual = SystemUnderTest.GeneratedClasses["Address"];

        expected = @"public class Address
{
    [JsonPropertyName(""Line1"")]
    public string Line1 { get; set; } = string.Empty;

    [JsonPropertyName(""Line2"")]
    public string Line2 { get; set; } = string.Empty;

    [JsonPropertyName(""City"")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName(""State"")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName(""PostalCode"")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName(""ThingyThing"")]
    public ThingyThing ThingyThing { get; set; } = new();
}";

        AssertCodeIsPrettyMuchEqual(expected, actual);

        actual = SystemUnderTest.GeneratedClasses["ThingyThing"];

        expected = @"public class ThingyThing
{
    [JsonPropertyName(""Title"")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName(""IsAwesome"")]
    public bool IsAwesome { get; set; }

    [JsonPropertyName(""FavoriteNumber"")]
    public int FavoriteNumber { get; set; }
}";

        AssertCodeIsPrettyMuchEqual(expected, actual);

    }

    [TestMethod]
    public void ParseComplex_Single()
    {
        // arrange
        var fromJson = GetComplexClassAsJson();

        var expectedClassCount = 3;
        var expectedClassNames = new List<string>()
        {
            "Person",
            "Address",
            "ThingyThing"
        };

        SystemUnderTest.RootClassName = "Person";

        // act
        SystemUnderTest.Parse(fromJson);

        // assert
        Assert.AreEqual<int>(expectedClassCount, SystemUnderTest.Classes.Count, $"Class count is wrong");

        expectedClassNames.ForEach(name =>
            Assert.IsTrue(SystemUnderTest.Classes.Keys.Contains(name), $"Class name '{name}' not found"));

        AssertAreEqual(GetClassInfoForPerson(), SystemUnderTest.Classes["Person"]);
        AssertAreEqual(GetClassInfoForThingy(), SystemUnderTest.Classes["ThingyThing"]);
        AssertAreEqual(GetClassInfoForAddress(), SystemUnderTest.Classes["Address"]);
    }

    [TestMethod]
    public void ParseComplex_Array()
    {
        // arrange
        var fromJson = GetComplexClassArrayAsJson();

        var expectedClassCount = 3;
        var expectedClassNames = new List<string>()
        {
            "Person",
            "Address",
            "ThingyThing"
        };

        SystemUnderTest.RootClassName = "Person";

        // act
        SystemUnderTest.Parse(fromJson);

        // assert
        Assert.AreEqual<int>(expectedClassCount, SystemUnderTest.Classes.Count, $"Class count is wrong");

        expectedClassNames.ForEach(name =>
            Assert.IsTrue(SystemUnderTest.Classes.Keys.Contains(name), $"Class name '{name}' not found"));

        AssertAreEqual(GetClassInfoForPerson(), SystemUnderTest.Classes["Person"]);
        AssertAreEqual(GetClassInfoForThingy(), SystemUnderTest.Classes["ThingyThing"]);
        AssertAreEqual(GetClassInfoForAddress(), SystemUnderTest.Classes["Address"]);
    }

    private ClassInfo GetClassInfoForThingy(string name = "ThingyThing")
    {
        var returnValue = new ClassInfo();

        returnValue.Name = name;

        returnValue.AddProperty("Title");
        returnValue.AddProperty("IsAwesome", "IsAwesome", "bool");
        returnValue.AddProperty("FavoriteNumber", "FavoriteNumber", "int");

        return returnValue;
    }

    private ClassInfo GetClassInfoForPerson(string name = "Person")
    {
        var returnValue = new ClassInfo();

        returnValue.Name = name;

        returnValue.AddProperty("Id", "Id", "int");
        returnValue.AddProperty("FirstName");
        returnValue.AddProperty("LastName");
        returnValue.AddProperty("IsAwesome", "IsAwesome", "bool");
        returnValue.AddProperty("FavoriteNumber", "FavoriteNumber", "int");
        returnValue.AddProperty("Addresses", "Addresses", "Address", true);

        return returnValue;
    }

    private ClassInfo GetClassInfoForAddress(string name = "Address")
    {
        var returnValue = new ClassInfo();

        returnValue.Name = name;

        returnValue.AddProperty("Line1");
        returnValue.AddProperty("Line2");
        returnValue.AddProperty("City");
        returnValue.AddProperty("State");
        returnValue.AddProperty("PostalCode");
        returnValue.AddProperty("ThingyThing", "ThingyThing", "ThingyThing");

        return returnValue;
    }
}
