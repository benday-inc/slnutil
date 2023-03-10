namespace Benday.SolutionUtil.UnitTests;

public static class UnitTestUtility
{
    public static string ReadTestFile()
    {
        var fullyQualifiedPath = GetPathToTestFile();

        var contents = File.ReadAllText(fullyQualifiedPath);

        return contents;
    }

    public static string GetPathToTestFile()
    {
        var pathToFile = Path.Combine("..", "work-items-and-scripts", "workitems-with-extra-states", "pbi-extra-states.xml");

        var workingDir = Environment.CurrentDirectory;

        Console.WriteLine($"original working dir: {Environment.CurrentDirectory}");

        workingDir = workingDir.Replace("/bin/Debug/net7.0", "");
        workingDir = workingDir.Replace("\\bin\\Debug\\net7.0", "");

        var fullyQualifiedPath = Path.GetFullPath(Path.Combine(workingDir, pathToFile));

        Console.WriteLine($"adjusted working dir: {workingDir}");
        Console.WriteLine($"pathToFile: {pathToFile}");
        Console.WriteLine($"fullyQualifiedPath: {fullyQualifiedPath}");

        Assert.AreEqual<bool>(true, File.Exists(fullyQualifiedPath),
            $"Path should exist. '{fullyQualifiedPath}'");

        return fullyQualifiedPath;
    }
}

