namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsExactTypeOfAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{
    public ThrowsExactTypeOfAssertCondition(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder, default)
    {
    }
    
    protected override string DefaultMessage => $"A {Exception?.GetType().Name} was thrown instead of {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        
        return exception.GetType() == typeof(TExpected);
    }
}