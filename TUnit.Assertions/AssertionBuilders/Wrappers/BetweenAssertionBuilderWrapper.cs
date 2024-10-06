using System.Numerics;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class BetweenAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual> where TActual : IComparable<TActual>
{
    internal BetweenAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public BetweenAssertionBuilderWrapper<TActual> Inclusive()
    {
        var assertion = (BetweenAssertCondition<TActual>) Assertions.Peek();

        assertion.Inclusive();
        
        return this;
    }
    
    public BetweenAssertionBuilderWrapper<TActual> Exclusive()
    {
        var assertion = (BetweenAssertCondition<TActual>) Assertions.Peek();

        assertion.Exclusive();
        
        return this;
    }
}