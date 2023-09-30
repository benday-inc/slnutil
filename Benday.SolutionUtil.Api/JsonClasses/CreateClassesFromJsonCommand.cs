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
        else
        {
            var generator = new JsonToClassGenerator();

            WriteLine("**** Input ****");
            WriteLine(json);
            WriteLine("**** Output ****");

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

    private static string GetJsonFromConsole()
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
