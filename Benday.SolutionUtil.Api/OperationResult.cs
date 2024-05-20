namespace Benday.SolutionUtil.Api;

public class OperationResult<T>
{
    public OperationResult(T value, bool valueChanged)
    {
        Value = value;
        ValueChanged = valueChanged;
    }
    
    public bool ValueChanged { get; private set; }
    
    public T Value { get; private set; }
}