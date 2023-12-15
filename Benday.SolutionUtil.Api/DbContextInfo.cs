using System.Reflection;

namespace Benday.SolutionUtil.Api;

public class DbContextInfo
{
    public Type? DbContextType { get; set; }
    public Assembly? Assembly { get; set; }
    public string AssemblyFileName { get; set; } = string.Empty;
}
