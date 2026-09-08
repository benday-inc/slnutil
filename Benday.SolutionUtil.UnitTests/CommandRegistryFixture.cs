using Benday.CommandsFramework;
using Benday.Common.Testing;
using Benday.SolutionUtil.Api;

using Xunit;

namespace Benday.SolutionUtil.UnitTests;

public class CommandRegistryFixture : TestClassBase
{
    public CommandRegistryFixture(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// Mirrors what Program.cs configures, so the registry under test is the one the
    /// tool actually builds at run time. UsesConfiguration in particular changes which
    /// commands are registered.
    /// </summary>
    private static DefaultProgramOptions GetProgramOptions()
    {
        return new DefaultProgramOptions
        {
            ApplicationName = "Solution & Project Utilities",
            Website = "https://www.benday.com",
            UsesConfiguration = false
        };
    }

    [Fact]
    public void CommandsHaveNoRegistryProblems()
    {
        var registry = CommandRegistry.Build(
            GetProgramOptions(), typeof(StringUtility).Assembly);

        // print them -- "Assert.Empty failed, collection had 3 items" does not say which
        foreach (var problem in registry.Problems)
        {
            WriteLine(problem);
        }

        Assert.Empty(registry.Problems);
    }

    [Fact]
    public void CommandsHaveNoArgumentProblems()
    {
        var utility = new CommandAttributeUtility(GetProgramOptions());

        var problems = utility.GetArgumentProblems(typeof(StringUtility).Assembly);

        foreach (var problem in problems)
        {
            WriteLine(problem);
        }

        Assert.Empty(problems);
    }
}
