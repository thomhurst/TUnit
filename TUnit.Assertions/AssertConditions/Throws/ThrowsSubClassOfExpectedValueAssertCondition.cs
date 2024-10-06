namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsSubClassOfExpectedValueAssertCondition<TActual, TExpected> : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetFailureMessage(Exception? exception) => $"A {exception?.GetType().Name} was thrown instead of subclass of {typeof(TExpected).Name}";

    protected override bool Passes(Exception? exception)
    {
        if (exception is null)
        {
            return FailWithMessage("Exception is null");
        }        
        
        return exception.GetType().IsSubclassOf(typeof(TExpected));
    }
}