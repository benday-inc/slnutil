namespace Benday.SolutionUtil.Api;

public static class Utilities
{
    public static void AssertNotNull<T>(T value, string valueName)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Value '{valueName}' was null.");
        }
    }

    public static void AssertFileExists(string path, string argumentName)
    {
        if (File.Exists(path) == false)
        {
            var info = new FileInfo(path);

            string message = String.Format(
                "File for argument '{0}' was not found at '{1}'.",
                argumentName,
                info.FullName);

            throw new FileNotFoundException(
                message, path);
        }
    }
}
