using System.Numerics;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NumberEqualToAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual> where TActual : INumber<TActual>
{
    internal NumberEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public NumberEqualToAssertionBuilderWrapper<TActual> Within(TActual tolerance)
    {
        var assertion = (NumericEqualsExpectedValueAssertCondition<TActual>) Assertions.Peek();

        assertion.SetTolerance(tolerance);
        
        return this;
    }
}