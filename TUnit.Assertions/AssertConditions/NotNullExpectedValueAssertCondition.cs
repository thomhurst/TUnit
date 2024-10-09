namespace TUnit.Assertions.AssertConditions;

public class NotNullExpectedValueAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    protected internal override string GetFailureMessage() => $"Member for {ActualExpression ?? typeof(TActual).Name} was null";
    
    protected override AssertionResult Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue is not null;
    }
}