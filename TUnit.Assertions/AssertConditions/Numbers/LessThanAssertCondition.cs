using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class LessThanAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected> 
    where TExpected : INumber<TExpected>
    where TActual : INumber<TActual>, TExpected
{
    public LessThanAssertCondition(AssertionBuilder<TActual> assertionBuilder, TExpected? expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => $"{ActualValue} is not less than {ExpectedValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(actualValue);
        
        return actualValue < ExpectedValue!;
    }
}