using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Benday.CommandsFramework;
using Benday.SolutionUtil.Api.JsonClasses;


namespace Benday.SolutionUtil.Api.Snippets;

[Command(
    Name = "snippetize",
    Description = "Reads a block of text from the clipboard and formats it for use in a VSCode snippet.")]
public class SnippetizeCommand : SynchronousCommand
{
    public SnippetizeCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
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
        var text = GetTextFromClipboard();

        if (string.IsNullOrWhiteSpace(text) == true)
        {
            throw new KnownException("Clipboard does not contain any text.");
        }

        var lines = GetAllLines(new StringReader(text));

        var snippet = new Snippet();
        snippet.Prefix = "yourPrefixHere";
        snippet.Body = lines;
        snippet.Description = "Your description here";

        var json = JsonSerializer.Serialize(snippet, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        WriteLine("\"snippet description\": " + json);
    }
    private string[] GetAllLines(StringReader reader)
    {
        var lines = new List<string>();

        string? line = null;

        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    private string GetTextFromClipboard()
    {
        if (OperatingSystem.IsWindows() == true)
        {
            return GetFromClipboardWindows();
        }
        else if (OperatingSystem.IsMacOS() == true)
        {
            return GetFromClipboardMacOs();
        }
        else
        {
            throw new KnownException("Clipboard reading is not supported on this OS.");
        }
    }

    private string GetFromClipboardWindows()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-command \"Get-Clipboard\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var runner = new ProcessRunner(startInfo);

        runner.Run();

        if (runner.IsError == true)
        {
            throw new KnownException($"Error reading from clipboard: {runner.ErrorText}");
        }
        else if (runner.IsTimeout == true)
        {
            throw new KnownException("Timeout reading from clipboard.");
        }
        else
        {
            return runner.OutputText.Trim();
        }
    }

    private string GetFromClipboardMacOs()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/usr/bin/pbpaste",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var runner = new ProcessRunner(startInfo);

        runner.Run();

        if (runner.IsError == true)
        {
            throw new KnownException($"Error reading from clipboard: {runner.ErrorText}");
        }
        else if (runner.IsTimeout == true)
        {
            throw new KnownException("Timeout reading from clipboard.");
        }
        else
        {
            return runner.OutputText.Trim();
        }
    }

    
}
