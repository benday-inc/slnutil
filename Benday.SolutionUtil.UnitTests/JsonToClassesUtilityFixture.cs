using Benday.SolutionUtil.Api;
using Benday.SolutionUtil.UnitTests.SampleClassesForSerialization;

namespace Benday.SolutionUtil.UnitTests;

[TestClass]
public class JsonToClassesUtilityFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private JsonToClassUtility? _SystemUnderTest;

    private JsonToClassUtility SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new JsonToClassUtility();
            }

            return _SystemUnderTest;
        }
    }


    [TestMethod]
    public void Serialize_Simple()
    {
        // arrange
        var item = new Thingy()
        {
            FavoriteNumber = 123,
            IsAwesome = true,
            Title = "asdf"
        };

        // act
        var actual = System.Text.Json.JsonSerializer.Serialize(item);

        // assert
        Console.WriteLine(actual);
    }

    [TestMethod]
    public void Serialize_Simple_Array()
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

        // assert
        Console.WriteLine(actual);
    }

}
