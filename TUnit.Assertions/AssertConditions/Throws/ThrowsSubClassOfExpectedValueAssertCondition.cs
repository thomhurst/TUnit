namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsSubClassOfExpectedValueAssertCondition<TActual, TExpected> : ExpectedExceptionAssertCondition<TActual>
{
    protected internal override string GetFailureMessage() => $"A {Exception?.GetType().Name} was thrown instead of subclass of {typeof(TExpected).Name}";

    protected override bool Passes(Exception? exception)
    {
        if (exception is null)
        {
            return FailWithMessage("Exception is null");
        }        
        
        return exception.GetType().IsSubclassOf(typeof(TExpected));
    }
}