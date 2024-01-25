namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceAssertCondition<T> : AssertCondition<T>
{
    private readonly T _expected;

    public SameReferenceAssertCondition(T expected) : base(expected)
    {
        _expected = expected;
    }

    public override bool Matches(T actualValue)
    {
        return ReferenceEquals(actualValue, _expected);
    }
    
    public override string Message { get; protected set; } = string.Empty;
}