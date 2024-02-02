namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingAssertCondition : AssertCondition<object, object>
{
    public ThrowsNothingAssertCondition(AssertionBuilder<object> assertionBuilder, object? expected) : base(assertionBuilder, expected)
    {
    }
    
    protected override string DefaultMessage => $"A {Exception?.GetType().Name} was thrown";

    protected internal override bool Passes(object? actualValue, Exception? exception)
    {
        return exception is null;
    }
}