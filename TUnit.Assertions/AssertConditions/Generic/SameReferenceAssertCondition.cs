namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceAssertCondition<T> : AssertCondition<T>
{
    private readonly T _expected;

    public SameReferenceAssertCondition(T expected) : base(expected)
    {
        _expected = expected;
    }

    internal override Func<(T ExpectedValue, T ActualValue), string> MessageFactory { get; set; }
        = tuple => $"The two objects are different references.";

    protected override bool Passes(T actualValue)
    {
        return ReferenceEquals(actualValue, _expected);
    }
}