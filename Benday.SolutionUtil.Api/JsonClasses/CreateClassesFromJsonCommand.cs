using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

using Benday.CommandsFramework;
using Benday.SolutionUtil.Api.JsonClasses;


namespace Benday.SolutionUtil.Api;

[Command(
    Name = Constants.CommandArgumentNameClassesFromJson,
    Description = "Create C# classes from JSON with serialization attributes for System.Text.Json.")]
public class CreateClassesFromJsonCommand : SynchronousCommand
{
    public CreateClassesFromJsonCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
            base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddFile(
            Constants.ArgumentNameFilename)
            .AsNotRequired()
            .WithDescription("Optional: file source for the JSON to convert to C# classes.")
            .FromPositionalArgument(1);

        args.AddBoolean("clipboard")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Optional: read JSON from clipboard instead of a file or console input.")
            .WithDefaultValue(false);

        return args;
    }


    protected override void OnExecute()
    {
        string json;

        var fileSourceHasValue = Arguments.HasValue(Constants.ArgumentNameFilename);
        var clipboardSourceHasValue = Arguments.GetBooleanValue("clipboard");
        if (clipboardSourceHasValue == true)
        {
            json = GetTextFromClipboard();

            if (string.IsNullOrWhiteSpace(json) == true)
            {
                throw new KnownException("Clipboard does not contain any text.");
            }
        }
        else if (fileSourceHasValue == true)
        {
            var sourceFile = Arguments.GetPathToFile(Constants.ArgumentNameFilename, true);

            json = File.ReadAllText(sourceFile);
        }
        else
        {
            json = GetJsonUsingFiles();
        }

        if (string.IsNullOrWhiteSpace(json) == true)
        {
            throw new KnownException("Input does not contain any text.");
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

                WriteClassesAndOpen(code.ToString());
            }
        }
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

    private void WriteClassesAndOpen(string code)
    {
        string tempDir = GetTempDirForApp();

        var tempFilename = $"generated-classes-from-json-{DateTime.Now.Ticks}.txt";
        var pathToOutputFile = Path.Combine(tempDir, tempFilename);

        File.WriteAllText(pathToOutputFile, code);

        OpenFileInTextEditor(tempDir, pathToOutputFile);
    }

    private string GetJsonUsingFiles()
    {
        string tempDir = GetTempDirForApp();

        var tempFilename = $"paste-json-to-this-file-and-save-{DateTime.Now.Ticks}.txt";

        string pathToInputFile = CreateEmptyFile(tempDir, tempFilename);

        WriteInstructionsToInputFile(tempDir, tempFilename);

        return OpenFileInTextEditorAndReadContentsAfterClose(tempDir, pathToInputFile);
    }

    private void WriteInstructionsToInputFile(string tempDir, string tempFilename)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Paste JSON into this file, save the file, and then exit this text editor program (or maybe just this tab).");
        builder.AppendLine("BTW, make sure you remove these instructions before saving the file.");
        builder.AppendLine();
        builder.AppendLine("When you exit this text editor, the JSON will be read from this file and converted to C# classes.");
        builder.AppendLine();
        builder.AppendLine("Yes, this is a bit of a hack.  I'm not sure how to get the JSON from the clipboard in a cross-platform way.");
        builder.AppendLine("If you have any suggestions, please let me know.  Thanks!");
        builder.AppendLine();

        File.WriteAllText(Path.Combine(tempDir, tempFilename), builder.ToString());
    }

    private void OpenFileInTextEditor(string tempDir, string pathToInputFile)
    {
        if (OperatingSystem.IsWindows() == true)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "notepad.exe",
                Arguments = pathToInputFile,
                CreateNoWindow = true,
                WorkingDirectory = tempDir
            };

            Process.Start(psi);
        }
        else
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "open",
                Arguments = $"-t {pathToInputFile}",
                CreateNoWindow = true,
                WorkingDirectory = tempDir
            };

            Process.Start(psi);
        }
    }

    private string OpenFileInTextEditorAndReadContentsAfterClose(string tempDir, string pathToInputFile)
    {
        if (OperatingSystem.IsWindows() == true)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "notepad.exe",
                Arguments = pathToInputFile,
                CreateNoWindow = true,
                WorkingDirectory = tempDir
            };

            Process.Start(psi)?.WaitForExit();

            var json = File.ReadAllText(pathToInputFile);

            return json;
        }
        else
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "open",
                Arguments = $"-t -F -W {pathToInputFile}",
                CreateNoWindow = true,
                WorkingDirectory = tempDir
            };

            Process.Start(psi)?.WaitForExit();

            var json = File.ReadAllText(pathToInputFile);

            return json;
        }
    }

    private static string CreateEmptyFile(string tempDir, string tempFilename)
    {
        var pathToInputFile = Path.Combine(tempDir, tempFilename);

        if (File.Exists(pathToInputFile) == false)
        {
            File.WriteAllText(pathToInputFile, string.Empty);
        }

        return pathToInputFile;
    }

    private static string GetTempDirForApp()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "slnutil");

        if (Directory.Exists(tempDir) == false)
        {
            Directory.CreateDirectory(tempDir);
        }

        return tempDir;
    }


}
