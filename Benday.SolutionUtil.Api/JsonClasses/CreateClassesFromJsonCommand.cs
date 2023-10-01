using System.Text;

using Benday.CommandsFramework;
using Benday.SolutionUtil.Api.JsonClasses;


namespace Benday.SolutionUtil.Api;

[Command(
    Name = Constants.CommandArgumentNameClassesFromJson,
    Description = "Create C# classes from JSON in clipboard with serialization attributes for System.Text.Json.")]
public class CreateClassesFromJsonCommand : SynchronousCommand
{
    public CreateClassesFromJsonCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
            base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        return args;
    }


    protected override void OnExecute()
    {
        string json = GetJsonFromConsole();

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            throw new KnownException("Input does not contain any text.");
        }
        else if (1 == 1)
        {
            WriteLine("");
            WriteLine(json);
        }
        else
        {
            var generator = new JsonToClassGenerator();

            generator.Parse(json);
            generator.GenerateClasses();

            if (generator.GeneratedClasses.Count == 0)
            {
                throw new KnownException("Input does not contain any classes.");
            }
            else
            {
                var code = new StringBuilder();

                foreach (var key in generator.GeneratedClasses.Keys)
                {
                    // WriteLine($"Generated class: {key}");
                    code.AppendLine(generator.GeneratedClasses[key]);
                    code.AppendLine();
                }

                // await Clipboard.SetTextAsync(code.ToString());

                // WriteLine("Classes set to clipboard");

                WriteLine(code.ToString());
            }
        }
    }

    private string GetJsonFromConsole()
    {
        // Console.Clear();
        WriteLine("Paste in the JSON string: ");

        /*
                byte[] inputBuffer = new byte[1024];
                Stream inputStream = Console.OpenStandardInput(inputBuffer.Length);
                Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, inputBuffer.Length));
                var strInput = Console.ReadLine();

                return strInput;
        */

        string password = string.Empty; // this will hold the password as it's being typed.

        int emptyLineCount = 0;
        int lineCountToExit = 4;

        var LINE_RESET = "\f\u001bc\x1b[3J";

        if (OperatingSystem.IsWindows() == true)
        {
            LINE_RESET = "\r";
        }

        long readCount = 0;

        while (true)
        {
            readCount++;
            var key = System.Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                emptyLineCount++;

                if (emptyLineCount >= lineCountToExit)
                {
                    break;
                }

                if (emptyLineCount > 0)
                {
                    Console.Write(LINE_RESET);
                    Console.SetCursorPosition(0, 1);
                    Console.Write($"Found {emptyLineCount} blank lines. Press Enter {(lineCountToExit - emptyLineCount)} more times to exit...");
                }

                password += Environment.NewLine;
            }
            else
            {
                emptyLineCount = 0;

                password += key.KeyChar;

                if (readCount % 100 == 0)
                {
                    Console.Write(LINE_RESET);
                    Console.SetCursorPosition(0, 1);
                    Console.Write($"Reading...                                                                                  ");
                }
            }
        }

        return password;

    }

    private static string GetJsonFromConsole2()
    {
        Console.WriteLine("Paste JSON from the clipboard and press enter three times: ");

        var lines = new StringBuilder();
        string? line;
        int emptyLineCount = 0;

        line = Console.ReadLine();

        while (line != null)
        {
            // Either you do here something with each line separately or
            lines.AppendLine(line);

            if (string.IsNullOrWhiteSpace(line) == true)
            {
                emptyLineCount++;
                if (emptyLineCount >= 3)
                {
                    break;
                }
                Console.WriteLine($"Empty line count: {emptyLineCount}");
            }
            else
            {
                emptyLineCount = 0;
            }


            line = Console.ReadLine();
        }

        var json = lines.ToString().Trim();
        return json;
    }
}
