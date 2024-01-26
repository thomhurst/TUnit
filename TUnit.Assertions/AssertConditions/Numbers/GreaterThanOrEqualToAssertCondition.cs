using System.Numerics;

namespace TUnit.Assertions;

public class GreaterThanOrEqualToAssertCondition<T> : AssertCondition<T> where T : INumber<T>
{
    public GreaterThanOrEqualToAssertCondition(T expected) : base(expected)
    {
    }

    internal override Func<(T ExpectedValue, T ActualValue), string> MessageFactory { get; set; }
        = tuple => $"{tuple.ActualValue} is not greater than or equal to {tuple.ExpectedValue}";
    
    protected override bool Passes(T actualValue)
    {
        return actualValue >= ExpectedValue;
    }
}