using Benday.SolutionUtil.Api.JsonClasses;
using Benday.SolutionUtil.UnitTests.SampleClassesForSerialization;

namespace Benday.SolutionUtil.UnitTests;

[TestClass]
public class JsonToClassesGeneratorFixture
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
        var expectedClassNames = new List<string>()
        {
            "ThingyThing"
        };

        // act
        SystemUnderTest.Parse(fromJson, "ThingyThing");

        // assert
        Assert.AreEqual<int>(expectedClassCount, SystemUnderTest.Classes.Count, $"Class count is wrong");

        expectedClassNames.ForEach(name =>
            Assert.IsTrue(SystemUnderTest.Classes.Keys.Contains(name), $"Class name '{name}' not found"));

        AssertAreEqual(GetClassInfoForThingy(),
            SystemUnderTest.Classes["ThingyThing"]);
    }
    private void AssertAreEqual(ClassInfo expected,
        ClassInfo actual)
    {
        Assert.AreEqual<string>(expected.Name, actual.Name, "Name");

        Assert.AreEqual<int>(expected.Properties.Count, actual.Properties.Count, "Properties.Count");

        AssertAreEqual(expected.Properties, actual.Properties);
    }

    private void AssertAreEqual(
        Dictionary<string, PropertyInfo> expected,
        Dictionary<string, PropertyInfo> actual)
    {
        var expectedKeys = expected.Keys;
        var actualKeys = actual.Keys;

        CollectionAssert.AreEqual(expectedKeys, actualKeys,
            "Key collections don't match.");

        foreach (var key in expectedKeys)
        {
            AssertAreEqual(expected[key], actual[key]);
        }

    }
    private void AssertAreEqual(PropertyInfo expected,
        PropertyInfo actual)
    {
        Assert.AreEqual<string>(expected.Name, actual.Name, "Name");
        Assert.AreEqual<string>(expected.DataType, actual.DataType, "DataType");
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
    public void ParseComplex_Single()
    {
        // arrange
        var fromJson = GetComplexClassAsJson();

        var expectedClassCount = 3;
        var expectedClassNames = new List<string>()
        {
            "RootClass",
            "Address",
            "ThingyThing"
        };

        // act
        SystemUnderTest.Parse(fromJson);

        // assert
        Assert.AreEqual<int>(expectedClassCount, SystemUnderTest.Classes.Count, $"Class count is wrong");

        expectedClassNames.ForEach(name =>
            Assert.IsTrue(SystemUnderTest.Classes.Keys.Contains(name), $"Class name '{name}' not found"));
    }

    [TestMethod]
    public void ParseComplex_Array()
    {
        // arrange
        var fromJson = GetComplexClassArrayAsJson();

        var expectedClassCount = 3;
        var expectedClassNames = new List<string>()
        {
            "RootClass",
            "Address",
            "ThingyThing"
        };

        // act
        SystemUnderTest.Parse(fromJson);

        // assert
        Assert.AreEqual<int>(expectedClassCount, SystemUnderTest.Classes.Count, $"Class count is wrong");

        expectedClassNames.ForEach(name =>
            Assert.IsTrue(SystemUnderTest.Classes.Keys.Contains(name), $"Class name '{name}' not found"));
    }

    private ClassInfo GetClassInfoForThingy(string name = "ThingyThing")
    {
        var returnValue = new ClassInfo();

        returnValue.Name = name;

        returnValue.AddProperty("Title");
        returnValue.AddProperty("IsAwesome", "bool");
        returnValue.AddProperty("FavoriteNumber", "int");

        return returnValue;
    }
}
