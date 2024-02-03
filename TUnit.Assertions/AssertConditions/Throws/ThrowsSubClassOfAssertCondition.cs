namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsSubClassOfAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{
    public ThrowsSubClassOfAssertCondition(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder, default)
    {
    }
    
    protected override string DefaultMessage => $"A {Exception?.GetType().Name} was thrown instead of subclass of {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        
        return exception.GetType().IsSubclassOf(typeof(TExpected));
    }
}