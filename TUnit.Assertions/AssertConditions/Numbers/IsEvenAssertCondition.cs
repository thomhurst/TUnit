using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class IsEvenAssertCondition<TActual> : AssertCondition<TActual, TActual>
    where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
{
    public IsEvenAssertCondition() : base(default)
    {
    }

    public override string DefaultMessage => $"{ActualValue} is not even";

    protected internal override bool Passes(TActual? actualValue)
    {
        ArgumentNullException.ThrowIfNull(actualValue);
        
        return actualValue % 2 == 0;
    }
}