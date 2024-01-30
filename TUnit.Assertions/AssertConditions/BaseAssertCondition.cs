using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions;

public abstract class BaseAssertCondition<TActual, TExpected>
{
    internal BaseAssertCondition()
    {
        And = new And<TActual, TExpected>(this);
        Or = new Or<TActual, TExpected>(this);
    }
    
    protected TActual ActualValue { get; set; } = default!;
    
    public abstract string Message { get; }
    
    public abstract string DefaultMessage { get; }
    
    public bool Assert(TActual actualValue)
    {
        ActualValue = actualValue;
        return Passes(actualValue);
    }

    protected internal abstract bool Passes(TActual actualValue);

    public And<TActual, TExpected> And { get; }
    public Or<TActual, TExpected> Or { get; }
}