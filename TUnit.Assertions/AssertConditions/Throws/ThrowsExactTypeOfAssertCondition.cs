namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsExactTypeOfAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{
    public ThrowsExactTypeOfAssertCondition() : base(default)
    {
    }
    
    protected override string DefaultMessage => $"A {Exception?.GetType().Name} was thrown instead of {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        if (exception is null)
        {
            WithMessage((_, _, _) => "Exception is null");
            return false;
        }
        
        return exception.GetType() == typeof(TExpected);
    }
}