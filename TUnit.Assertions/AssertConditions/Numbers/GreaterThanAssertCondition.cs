using System.Numerics;

namespace TUnit.Assertions;

public class GreaterThanAssertCondition<T> : AssertCondition<T> where T : INumber<T>
{
    public GreaterThanAssertCondition(T expected) : base(expected)
    {
    }

    internal override Func<(T ExpectedValue, T ActualValue), string> MessageFactory { get; set; }
        = tuple => $"{tuple.ActualValue} is not greater than {tuple.ExpectedValue}";
    
    protected override bool Passes(T actualValue)
    {
        return actualValue > ExpectedValue;
    }
}