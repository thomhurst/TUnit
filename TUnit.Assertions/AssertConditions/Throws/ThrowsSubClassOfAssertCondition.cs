namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsSubClassOfAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{
    public ThrowsSubClassOfAssertCondition() : base(default)
    {
    }
    
    protected internal override string GetFailureMessage() => $"A {Exception?.GetType().Name} was thrown instead of subclass of {typeof(TExpected).Name}";

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (exception is null)
        {
            OverriddenMessage = "Exception is null";
            return false;
        }        
        
        return exception.GetType().IsSubclassOf(typeof(TExpected));
    }
}