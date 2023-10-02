namespace Benday.SolutionUtil.Api;

public class ReferenceInfo
{

    public string ReferenceType { get; set; } = string.Empty;
    private string _ReferenceTarget = string.Empty;
    public string ReferenceTarget
    {
        get => _ReferenceTarget;
        set
        {
            _ReferenceTarget = CleanCommas(value);
            ReferenceTargetRaw = value;
        }
    }

    public string ReferenceTargetRaw
    {
        get;
        private set;
    } = string.Empty;

    private string CleanCommas(string value)
    {

        if (value.Contains(",") == false)
        {
            return value;
        }
        else
        {
            var tokens = value.Split(',');

            if (tokens.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return tokens[0];
            }
        }

    }
}
