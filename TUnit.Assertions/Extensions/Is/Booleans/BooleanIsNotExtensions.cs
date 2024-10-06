#nullable disable

using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class BooleanIsNotExtensions
{
    public static InvokableValueAssertionBuilder<bool> IsNotTrue(this IValueSource<bool> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool>(false)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<bool> IsNotFalse(this IValueSource<bool> valueSource)
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<bool>(true)
            , []);
    }
}