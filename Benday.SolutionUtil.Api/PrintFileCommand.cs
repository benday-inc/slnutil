using Benday.CommandsFramework;

namespace Benday.SolutionUtil.Api;

[Command(
    Name = "printfile",
    Description = "Reads a text file and prints it to the console character by character. This is helpful for diagnosing encoding issues and weird text issues.",
    IsAsync = true)]
public class PrintFileCommand : AsynchronousCommand
{
    public PrintFileCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider) : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var arguments = new ArgumentCollection();

        arguments.AddFile("input")
            .AsRequired()
            .MustExist()
            .FromPositionalArgument(1)
            .WithDescription("Path to the text file to read and print.");

        return arguments;
    }

    protected override Task OnExecute()
    {
        var inputFilePath = Arguments.GetPathToFile("input", true, true);

        var fullContents = File.ReadAllText(inputFilePath);
        WriteLine(fullContents);
        WriteLine();
        WriteLine("Character by character:");

        using var reader = new StreamReader(inputFilePath);

        var charNumber = 0;

        while (reader.EndOfStream == false)
        {
            charNumber++;
            var paddedCharNumber = charNumber.ToString().PadLeft(4, '0');
            var asCharacter = (char)reader.Read();
            int asciiValue = (int)asCharacter;

            WriteLine($"[{paddedCharNumber}] '{asCharacter}' -> {asciiValue}");
        }

        return Task.CompletedTask;
    }

}