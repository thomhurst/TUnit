namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<T>
{
    internal AssertCondition()
    {
        And = new And<T>([this]);
        Or = new Or<T>([this]);
    }

    private Func<T, string>? MessageFactory { get; set; }

    protected T ActualValue { get; private set; } = default!;

    public bool Assert(T actualValue)
    {
        ActualValue = actualValue;
        return Passes(actualValue);
    }

    public abstract string DefaultMessage { get; }

    protected abstract bool Passes(T actualValue);

    public string Message => MessageFactory?.Invoke(ActualValue) ?? DefaultMessage;
    
    public AssertCondition<T> WithMessage(Func<T, string> messageFactory)
    {
        MessageFactory = messageFactory;
        return this;
    }

    public And<T> And { get; }
    public Or<T> Or { get; }
}