namespace TUnit.Assertions;

public interface IAssertCondition<T>
{
    internal T ExpectedValue { get; }
    
    public bool Matches(T actualValue);
    
    internal string Message { get; }
}