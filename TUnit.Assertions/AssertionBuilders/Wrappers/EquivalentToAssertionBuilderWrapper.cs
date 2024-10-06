using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class EquivalentToAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual>
{
    internal EquivalentToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }
    
    public EquivalentToAssertionBuilderWrapper<TActual> IgnoringMember(string propertyName)
    {
        var assertion = (EquivalentToExpectedValueAssertCondition<TActual>) Assertions.Peek();

        assertion.IgnoringMember(propertyName);
        
        return this;
    }
}