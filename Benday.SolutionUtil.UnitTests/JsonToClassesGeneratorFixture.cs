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
            "RootClass"
        };

        // act
        SystemUnderTest.Parse(fromJson);

        // assert
        Assert.AreEqual<int>(expectedClassCount, SystemUnderTest.Classes.Count, $"Class count is wrong");

        expectedClassNames.ForEach(name =>
            Assert.IsTrue(SystemUnderTest.Classes.Keys.Contains(name), $"Class name '{name}' not found"));
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
}
