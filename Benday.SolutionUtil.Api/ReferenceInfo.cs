namespace Benday.SolutionUtil.Api;

public class ReferenceInfo
{

    public string ReferenceType { get; set; }
    private string _ReferenceTarget;
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
    }

    private string CleanCommas(string value)
    {
        if (value == null)
        {
            return value;
        }
        else
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
}
