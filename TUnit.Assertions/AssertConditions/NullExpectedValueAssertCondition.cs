namespace TUnit.Assertions.AssertConditions;

public class NullExpectedValueAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    protected internal override string GetFailureMessage() => $"{ActualValue} is not null";
    
    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return actualValue is null;
    }
}