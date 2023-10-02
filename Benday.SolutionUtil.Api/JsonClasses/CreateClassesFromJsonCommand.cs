﻿using System.Diagnostics;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
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
        string json = GetJsonUsingFiles();

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

                WriteClasesAndOpen(code.ToString());
            }
        }
    }

    private void WriteClasesAndOpen(string code)
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

        builder.AppendLine("Paste JSON into this file, save the file, and then exit this text editor program.");
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

            WriteLine("exited...");

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

            WriteLine("exited...");

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
}
