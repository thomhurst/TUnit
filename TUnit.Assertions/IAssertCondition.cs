namespace TUnit.Assertions;

public interface IAssertCondition<in T>
{
    public bool Assert(T actualValue);
    
    internal string Message { get; }
}