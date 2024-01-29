namespace TUnit.Assertions.AssertConditions.Conditions;

public class ThrowsNothingAsyncAssertCondition : AsyncAssertCondition
{
    public override string DefaultMessage => $"A {Exception?.GetType().Name} was thrown";
    
    protected override bool Passes(Exception? exception)
    {
        return Exception == null;
    }
}