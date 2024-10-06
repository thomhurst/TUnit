namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsExactTypeOfDelegateAssertCondition<TActual, TExpectedException> : DelegateAssertCondition<TActual, TExpectedException> 
    where TExpectedException : Exception
{
    protected override string GetFailureMessage(TExpectedException? exception) => $"A {exception?.GetType().Name} was thrown instead of {typeof(TExpectedException).Name}";

    protected override bool Passes(TExpectedException? exception)
    {
        if (exception is null)
        {
            return FailWithMessage("Exception is null");
        }
        
        return exception.GetType() == typeof(TExpectedException);
    }
}