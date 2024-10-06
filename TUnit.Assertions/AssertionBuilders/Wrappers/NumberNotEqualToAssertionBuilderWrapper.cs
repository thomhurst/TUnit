using System.Numerics;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NumberNotEqualToAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual> where TActual : INumber<TActual>
{
    internal NumberNotEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public NumberNotEqualToAssertionBuilderWrapper<TActual> Within(TActual tolerance)
    {
        var assertion = (NumericNotEqualAssertCondition<TActual>) Assertions.Peek();

        assertion.SetTolerance(tolerance);
        
        return this;
    }
}