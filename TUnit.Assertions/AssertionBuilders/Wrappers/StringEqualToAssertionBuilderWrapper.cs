using System.Numerics;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class StringEqualToAssertionBuilderWrapper : InvokableValueAssertionBuilder<string>
{
    internal StringEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<string> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public StringEqualToAssertionBuilderWrapper Within(string tolerance)
    {
        var assertion = (StringEqualsAssertCondition) Assertions.Peek();

        assertion.SetTolerance(tolerance);
        
        return this;
    }
}