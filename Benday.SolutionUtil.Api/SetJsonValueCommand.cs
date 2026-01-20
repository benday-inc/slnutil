using Benday.CommandsFramework;
using Benday.JsonUtilities;

namespace Benday.SolutionUtil.Api;

[Command(Name = Constants.CommandArgumentNameSetJsonValue,
    Description = "Set a string value in a json file.")]
public class SetJsonValueCommand : SynchronousCommand
{
    public SetJsonValueCommand(CommandExecutionInfo info, ITextOutputProvider outputProvider) :
        base(info, outputProvider)
    {

    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString(Constants.ArgumentNameConfigFilename)
            .AsNotRequired()
            .WithDescription("Path to json config file");

        args.AddString(Constants.ArgumentNameLevel1)
            .AsRequired()
            .WithDescription("First level json property name to set");

        args.AddString(Constants.ArgumentNameLevel2)
            .AsNotRequired()
            .WithDescription("Second level json property name to set");

        args.AddString(Constants.ArgumentNameLevel3)
            .AsNotRequired()
            .WithDescription("Third level json property name to set");

        args.AddString(Constants.ArgumentNameLevel4)
            .AsNotRequired()
            .WithDescription("Fourth level json property name to set");

        args.AddString(Constants.ArgumentNameValue)
            .AsNotRequired()
            .WithDefaultValue(string.Empty)
            .WithDescription("String value to set");

        args.AddBoolean(Constants.ArgumentNameIncrementInt32Value)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription(
                $"Increment the existing value or use the '/{Constants.ArgumentNameValue}' as the value if it does not exist or isn't an integer.");

        args.AddBoolean(Constants.ArgumentNameIncrementMinorVersionValue)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription(
                $"Increment the minor version of the existing value or use the '/{Constants.ArgumentNameValue}' as the value if it does not exist or isn't an integer.");

        args.AddBoolean(Constants.ArgumentNameIncrementPatchVersionValue)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription(
                $"Increment the patch version of the existing value or use the '/{Constants.ArgumentNameValue}' as the value if it does not exist or isn't an integer.");

        args.AddBoolean(Constants.ArgumentNameSetValueAsBoolean)
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription($"Set value into the json as boolean")
            .WithDefaultValue(false);

        return args;
    }

    protected override void OnExecute()
    {
        string? configFilename;

        if (Arguments.HasValue(Constants.ArgumentNameConfigFilename) == true)
        {
            configFilename = Arguments.GetStringValue(Constants.ArgumentNameConfigFilename);
        }
        else
        {
            configFilename =
                ProjectUtilities.FindFirstFileName(
                    Environment.CurrentDirectory, "appsettings.json");
        }

        if (configFilename == null)
        {
            throw new KnownException("Could not find appsettings.json file");
        }
        else
        {
            Utilities.AssertFileExists(configFilename, Constants.ArgumentNameConfigFilename);
        }

        WriteLine($"Using '{configFilename}'...");

        var setAsBoolean = Arguments.GetBooleanValue(Constants.ArgumentNameSetValueAsBoolean);
        var incrementMinorVersionValue = Arguments.GetBooleanValue(Constants.ArgumentNameIncrementMinorVersionValue);
        var incrementPatchVersionValue = Arguments.GetBooleanValue(Constants.ArgumentNameIncrementPatchVersionValue);

        if (incrementMinorVersionValue == true &&
            incrementPatchVersionValue == true)
        {
            throw new KnownException(
                $"You cannot specify both '/{Constants.ArgumentNameIncrementMinorVersionValue}' and '/{Constants.ArgumentNameIncrementPatchVersionValue}' at the same time.");
        }

        var newValue = Arguments.GetStringValue(Constants.ArgumentNameValue);
        var level1 = Arguments.GetStringValue(Constants.ArgumentNameLevel1);
        string? level2 = null;
        string? level3 = null;
        string? level4 = null;

        var editor = new JsonEditor(configFilename);

        if (Arguments.HasValue(Constants.ArgumentNameLevel2) == true &&
            Arguments.HasValue(Constants.ArgumentNameLevel3) == true &&
            Arguments.HasValue(Constants.ArgumentNameLevel4) == true)
        {
            level2 = Arguments.GetStringValue(Constants.ArgumentNameLevel2);
            level3 = Arguments.GetStringValue(Constants.ArgumentNameLevel3);
            level4 = Arguments.GetStringValue(Constants.ArgumentNameLevel4);

            SetValue(
                editor, setAsBoolean,
                newValue, level1, level2, level3, level4);
        }
        else if (Arguments.HasValue(Constants.ArgumentNameLevel2) == true &&
            Arguments.HasValue(Constants.ArgumentNameLevel3) == true)
        {
            level2 = Arguments.GetStringValue(Constants.ArgumentNameLevel2);
            level3 = Arguments.GetStringValue(Constants.ArgumentNameLevel3);

            SetValue(
                editor, setAsBoolean,
                newValue, level1, level2, level3);
        }
        else if (Arguments.HasValue(Constants.ArgumentNameLevel2) == true &&
            Arguments.HasValue(Constants.ArgumentNameLevel3) == false)
        {
            level2 = Arguments.GetStringValue(Constants.ArgumentNameLevel2);

            SetValue(
                editor, setAsBoolean,
                newValue,
                level1, level2);
        }
        else
        {
            SetValue(editor, setAsBoolean, newValue, level1);
        }

        var json = editor.ToJson(true);

        File.WriteAllText(configFilename, json);

        WriteLine($"Updated '{configFilename}'.");
    }

    private void SetValue(JsonEditor editor, bool setAsBoolean,
        string newValue, params string[] levels)
    {
        var currentValue = editor.GetValue(levels);

        newValue = ProcessIncrementIfExists(currentValue, newValue);

        if (setAsBoolean == true)
        {
            // try parse the value as a boolean

            if (bool.TryParse(newValue, out var valueAsBoolean) == false)
            {
                throw new KnownException(
                    $"Could not parse '{newValue}' as a boolean value.");
            }
            else
            {
                editor.SetValue(
                    bool.Parse(newValue),
                    levels);
            }
        }
        else
        {
            editor.SetValue(
                newValue,
                levels);
        }
    }

    private string ProcessIncrementIfExists(string? currentValue, string newValue)
    {
        var incrementInt32Value = Arguments.GetBooleanValue(Constants.ArgumentNameIncrementInt32Value);
        var incrementMinorVersionValue = Arguments.GetBooleanValue(Constants.ArgumentNameIncrementMinorVersionValue);
        var incrementPatchVersionValue = Arguments.GetBooleanValue(Constants.ArgumentNameIncrementPatchVersionValue);

        if (incrementInt32Value == true)
        {
            if (!string.IsNullOrEmpty(currentValue) && Int32.TryParse(currentValue, out var valueAsInt))
            {
                return (++valueAsInt).ToString();
            }
            else
            {
                WriteLine($"Warning: Could not increment '{currentValue}' as an int. Setting value to '{newValue}'.");
            }
        }
        else if (incrementMinorVersionValue == true)
        {
            if (string.IsNullOrEmpty(currentValue) == false)
            {
                var tokens = currentValue.Split('.');

                if (tokens.Length < 2)
                {
                    WriteLine($"Warning: Could not find minor version value in '{currentValue}'. Setting value to '{newValue}'.");
                }
                else
                {
                    var minorVersionAsString = tokens[1];

                    if (Int32.TryParse(minorVersionAsString, out var valueAsInt) == true)
                    {
                        valueAsInt++;

                        tokens[1] = valueAsInt.ToString();

                        var returnValue = string.Join(".", tokens);

                        return returnValue;
                    }
                    else
                    {
                        WriteLine($"Warning: Could not increment minor version value in '{currentValue}' as int. Setting value to '{newValue}'.");
                    }
                }
            }
        }
        else if (incrementPatchVersionValue == true)
        {
            if (string.IsNullOrEmpty(currentValue) == false)
            {
                var tokens = currentValue.Split('.');

                if (tokens.Length < 3)
                {
                    WriteLine($"Warning: Could not find patch version value in '{currentValue}'. Setting value to '{newValue}'.");
                }
                else
                {
                    var patchVersionAsString = tokens[2];

                    if (Int32.TryParse(patchVersionAsString, out var valueAsInt) == true)
                    {
                        valueAsInt++;

                        tokens[2] = valueAsInt.ToString();

                        var returnValue = string.Join(".", tokens);

                        return returnValue;
                    }
                    else
                    {
                        WriteLine($"Warning: Could not increment patch version value in '{currentValue}' as int. Setting value to '{newValue}'.");
                    }
                }
            }
        }

        return newValue;
    }

    private void SetValue(JsonEditor editor, string newValue, string level1)
    {
        var currentValue = editor.GetValue(level1);

        newValue = ProcessIncrementIfExists(currentValue, newValue);

        editor.SetValue(
                newValue,
                level1);
    }

    protected void AssertFileExists(string path, string argumentName)
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
