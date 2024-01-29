namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<T>
{
    internal readonly IReadOnlyCollection<AssertCondition<T>> PreviousAssertConditions;
    
    internal AssertCondition(IReadOnlyCollection<AssertCondition<T>> previousAssertConditions)
    {
        IReadOnlyCollection<AssertCondition<T>> conditionsUntilNow = [..previousAssertConditions, this];
        PreviousAssertConditions = conditionsUntilNow;
        
        And = new And<T>(conditionsUntilNow);
        Or = new Or<T>(conditionsUntilNow);
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