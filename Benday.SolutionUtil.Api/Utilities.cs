using System.Text;

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

    public static void AssertDirectoryExists(string path, string argumentName)
    {
        if (Directory.Exists(path) == false)
        {
            var info = new DirectoryInfo(path);

            string message = String.Format(
                "Directory for argument '{0}' was not found at '{1}'.",
                argumentName,
                info.FullName);

            throw new DirectoryNotFoundException(
                $"{message}");
        }
    }

    public static int FindNumberOfMatchingCharacters(string originalFileName, string fileName)
    {
        var originalFileNameLength = originalFileName.Length;

        var matchingCharCount = 0;

        for (int i = 0; i < fileName.Length; i++)
        {
            if (i >= originalFileNameLength)
            {
                return i;
            }
            else if (originalFileName[i] != fileName[i])
            {
                return i;
            }
            else
            {
                matchingCharCount++;
            }
        }

        return matchingCharCount;
    }
    public static string JsonNameToCsharpName(string input)
    {
        var returnValue = RemoveUnderscoreToPascalCase('_', input);

        returnValue = RemoveUnderscoreToPascalCase(' ', returnValue);

        returnValue = RemoveUnderscoreToPascalCase('-', returnValue);

        return returnValue;
    }

    public static string ToCapitalized(string fromValue)
    {
        if (string.IsNullOrWhiteSpace(fromValue) == true)
        {
            return string.Empty;
        }
        else
        {
            var returnValue = fromValue.Trim();

            var builder = new StringBuilder();

            var isFirstChar = true;

            foreach (var currentChar in returnValue)
            {
                if (isFirstChar == true)
                {
                    builder.Append(char.ToUpper(currentChar));
                }
                else
                {
                    builder.Append(currentChar);
                }

                isFirstChar = false;
            }

            return builder.ToString();
        }
    }

    public static string RemoveUnderscoreToPascalCase(char replaceCharacter, string fromValue)
    {
        if (fromValue == null)
        {
            return string.Empty;
        }
        else
        {
            fromValue = fromValue.Trim();

            var tokens = fromValue.Split(replaceCharacter);

            if (tokens.Length == 0)
            {
                return ToCapitalized(fromValue);
            }
            else
            {
                var builder = new StringBuilder();

                foreach (var token in tokens)
                {
                    builder.Append(ToCapitalized(token));
                }

                return builder.ToString();
            }
        }
    }
}
