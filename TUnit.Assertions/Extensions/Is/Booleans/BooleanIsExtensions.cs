#nullable disable

using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class BooleanIsExtensions
{
    public static InvokableValueAssertionBuilder<bool> IsTrue(this IValueSource<bool> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool>(true)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<bool> IsFalse(this IValueSource<bool> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool>(false)
            , []);
    }
}