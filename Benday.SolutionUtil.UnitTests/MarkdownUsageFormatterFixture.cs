using Benday.CommandsFramework;
using Benday.Common.Testing;
using Benday.SolutionUtil.Api.UsageFormatters;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class MarkdownUsageFormatterFixture : TestClassBase
{
    public MarkdownUsageFormatterFixture(ITestOutputHelper output) : base(output)
    {
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
    public DefaultProgramOptions GetProgramOptions()
    {
        var options = new DefaultProgramOptions();

        options.ApplicationName = "Solution & Project Utilities";
        options.Website = "https://www.benday.com";
        options.UsesConfiguration = false;

        return options;
    }

    [Fact]
    public void FormatUsagesAsMarkdown_GitHubReadme()
    {
        // arrange
        var assembly = typeof(StringUtility).Assembly;

        var usages = new CommandAttributeUtility(GetProgramOptions()).GetAllCommandUsages(assembly);

        // act
        var actual = SystemUnderTest.Format(usages, false);

        // assert
        AssertThat.IsNotNull(actual, "actual markdown was null");
        AssertThat.IsFalse(string.IsNullOrEmpty(actual), "actual markdown was empty");

        var filename = Path.Combine(Path.GetTempPath(), $"markdown-{DateTime.Now.Ticks}.md");

        WriteLine($"Writing markdown file to {Environment.NewLine}{filename}");

        File.WriteAllText(filename, actual);

        // WriteLine($"{actual}");
    }

    [Fact]
    public void FormatUsagesAsMarkdown_NuGetReadme_NoIntraDocumentAnchors()
    {
        // arrange
        var assembly = typeof(StringUtility).Assembly;

        var usages = new CommandAttributeUtility(GetProgramOptions()).GetAllCommandUsages(assembly);

        // act
        var actual = SystemUnderTest.Format(usages, true);

        // assert
        AssertThat.IsNotNull(actual, "actual markdown was null");
        AssertThat.IsFalse(string.IsNullOrEmpty(actual), "actual markdown was empty");

        var filename = Path.Combine(Path.GetTempPath(), $"markdown-{DateTime.Now.Ticks}.md");

        WriteLine($"Writing markdown file to {Environment.NewLine}{filename}");

        File.WriteAllText(filename, actual);

        // WriteLine($"{actual}");
    }

    [Fact]
    public void GenerateReadmeFiles()
    {
        // arrange
        var assembly = typeof(StringUtility).Assembly;

        var usages = new CommandAttributeUtility(GetProgramOptions()).GetAllCommandUsages(assembly);

        var solutionDir = GetPathToSolutionRootDirectory();

        WriteLine($"solutionDir: {solutionDir}");

        var miscDir = GetPathToMiscDirectory();

        var pathToGeneratedReadmeFiles = Path.Combine(solutionDir, "generated-readme-files");

        if (Directory.Exists(pathToGeneratedReadmeFiles) == false)
        {
            Directory.CreateDirectory(pathToGeneratedReadmeFiles);
        }

        var readmeHeader = File.ReadAllText(Path.Combine(miscDir, "readme-header.md"));

        var readmeCommandsForNuget = SystemUnderTest.Format(usages, true);
        var readmeCommandsForGitHub = SystemUnderTest.Format(usages, false);

        // act
        string pathToNugetReadme = Path.Combine(pathToGeneratedReadmeFiles, "README-for-nuget.md");
        string pathToGitHubReadme = Path.Combine(pathToGeneratedReadmeFiles, "README.md");

        WriteLine($"pathToNugetReadme: {pathToNugetReadme}");
        WriteLine($"pathToGitHubReadme: {pathToGitHubReadme}");

        File.WriteAllText(pathToNugetReadme,
            readmeHeader + Environment.NewLine + readmeCommandsForNuget
            );



        File.WriteAllText(pathToGitHubReadme,
            readmeHeader + Environment.NewLine + readmeCommandsForGitHub
            );
    }

    public string GetPathToSolutionRootDirectory()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        var assemblyPath = assembly.Location;

        var assemblyDir = Path.GetDirectoryName(assemblyPath);

        WriteLine($"assembly dir: {assemblyDir}");

        var relativePathToConfig = "..\\..\\..\\..\\".Replace('\\', Path.DirectorySeparatorChar);

        var pathToDir = Path.Combine(assemblyDir!, relativePathToConfig);

        var dirInfo = new DirectoryInfo(pathToDir);

        WriteLine($"Misc directory: {dirInfo.FullName}");

        AssertThat.IsTrue(Directory.Exists(pathToDir), $"Could not locate directory at '{pathToDir}'.");

        return dirInfo.FullName;
    }


    public string GetPathToMiscDirectory()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        var assemblyPath = assembly.Location;

        var assemblyDir = Path.GetDirectoryName(assemblyPath);

        WriteLine($"assembly dir: {assemblyDir}");

        var relativePathToConfig = "..\\..\\..\\..\\misc\\".Replace('\\', Path.DirectorySeparatorChar);

        var pathToDir = Path.Combine(assemblyDir!, relativePathToConfig);

        var dirInfo = new DirectoryInfo(pathToDir);

        WriteLine($"Misc directory: {dirInfo.FullName}");

        AssertThat.IsTrue(Directory.Exists(pathToDir), $"Could not locate directory at '{pathToDir}' -- '{new DirectoryInfo(pathToDir).FullName}'.");

        return dirInfo.FullName;
    }
}
