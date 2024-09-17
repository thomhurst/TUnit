namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsExactTypeOfAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{
    public ThrowsExactTypeOfAssertCondition() : base(default)
    {
    }
    
    protected internal override string GetFailureMessage() => $"A {Exception?.GetType().Name} was thrown instead of {typeof(TExpected).Name}";

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (exception is null)
        {
            OverriddenMessage = "Exception is null";
            return false;
        }
        
        return exception.GetType() == typeof(TExpected);
    }
}