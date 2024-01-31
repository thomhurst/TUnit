using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public class NullAssertCondition : AssertCondition<object,object>
{
    public NullAssertCondition() : base(null)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not null";
    protected internal override bool Passes(object? actualValue)
    {
        return actualValue is null;
    }
}