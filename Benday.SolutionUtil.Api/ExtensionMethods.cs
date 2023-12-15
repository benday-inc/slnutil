using System.Reflection;

namespace Benday.SolutionUtil.Api;

public static class ExtensionMethods
{
    public static IEnumerable<Type?> GetLoadableTypes(this Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }
}