using Benday.CommandsFramework;
using Benday.SolutionUtil.Api.UsageFormatters;

namespace Benday.SolutionUtil.UnitTests;

[TestClass]
public class MarkdownUsageFormatterFixture
{
    [TestInitialize]
    public void OnTestInitialize()
    {
        _SystemUnderTest = null;
    }

    private MarkdownUsageFormatter? _SystemUnderTest;

    private MarkdownUsageFormatter SystemUnderTest
    {
        get
        {
            if (_SystemUnderTest == null)
            {
                _SystemUnderTest = new MarkdownUsageFormatter();
            }

            return _SystemUnderTest;
        }
    }

    [TestMethod]
    public void FormatUsagesAsMarkdown_GitHubReadme()
    {
        // arrange
        var assembly = typeof(StringUtility).Assembly;

        var usages = new CommandAttributeUtility().GetAllCommandUsages(assembly);

        // act
        var actual = SystemUnderTest.Format(usages, false);

        // assert
        Assert.IsNotNull(actual, "actual markdown was null");
        Assert.IsFalse(string.IsNullOrEmpty(actual), "actual markdown was empty");

        var filename = Path.Combine(Path.GetTempPath(), $"markdown-{DateTime.Now.Ticks}.md");

        Console.WriteLine($"Writing markdown file to {Environment.NewLine}{filename}");

        File.WriteAllText(filename, actual);

        // Console.WriteLine($"{actual}");
    }

    [TestMethod]
    public void FormatUsagesAsMarkdown_NuGetReadme_NoIntraDocumentAnchors()
    {
        // arrange
        var assembly = typeof(StringUtility).Assembly;

        var usages = new CommandAttributeUtility().GetAllCommandUsages(assembly);

        // act
        var actual = SystemUnderTest.Format(usages, true);

        // assert
        Assert.IsNotNull(actual, "actual markdown was null");
        Assert.IsFalse(string.IsNullOrEmpty(actual), "actual markdown was empty");

        var filename = Path.Combine(Path.GetTempPath(), $"markdown-{DateTime.Now.Ticks}.md");

        Console.WriteLine($"Writing markdown file to {Environment.NewLine}{filename}");

        File.WriteAllText(filename, actual);

        // Console.WriteLine($"{actual}");
    }
}
