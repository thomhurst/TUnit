namespace TUnit.Assertions;

public abstract class AssertCondition<T> : IAssertCondition<T>
{
    internal AssertCondition(T expected)
    {
        ExpectedValue = expected;
    }
    
    internal Func<(T expectedValue, T actualValue), string>? MessageFactory { get; private set; }

    public T ExpectedValue { get; }
    public abstract bool Matches(T actualValue);
    
    public abstract string Message { get; protected set; }
    
    public IAssertCondition<T> WithMessage(Func<(T expectedValue, T actualValue), string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }
}