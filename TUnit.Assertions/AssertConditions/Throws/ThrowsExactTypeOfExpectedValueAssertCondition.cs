namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsExactTypeOfExpectedExceptionAssertCondition<TActual, TExpectedException> : ExpectedExceptionAssertCondition<TActual>
{
    protected internal override string GetFailureMessage() => $"A {Exception?.GetType().Name} was thrown instead of {typeof(TExpectedException).Name}";

    protected override bool Passes(Exception? exception)
    {
        if (exception is null)
        {
            OverriddenMessage = "Exception is null";
            return false;
        }
        
        return exception.GetType() == typeof(TExpectedException);
    }
}