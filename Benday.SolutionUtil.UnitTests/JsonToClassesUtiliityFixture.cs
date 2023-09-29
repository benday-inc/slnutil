using Benday.SolutionUtil.Api;

namespace Benday.SolutionUtil.UnitTests;

[TestClass]
public class JsonToClassesUtiliityFixture
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

   


}
