namespace TUnit.Assertions;

public class NotNullAssertCondition : NullAssertCondition
{
    protected override string DefaultMessage => "Value is null";
    protected internal override bool Passes(object? actualValue)
    {
        return !base.Passes(actualValue);
    }
}