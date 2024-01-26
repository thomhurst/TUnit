using System.Numerics;

namespace TUnit.Assertions;

public class LessThanOrEqualToAssertCondition<T> : AssertCondition<T> where T : INumber<T>
{
    public LessThanOrEqualToAssertCondition(T expected) : base(expected)
    {
    }

    internal override Func<(T ExpectedValue, T ActualValue), string> MessageFactory { get; set; }
        = tuple => $"{tuple.ActualValue} is not less than or equal to {tuple.ExpectedValue}";
    
    protected override bool Passes(T actualValue)
    {
        return actualValue <= ExpectedValue;
    }
}