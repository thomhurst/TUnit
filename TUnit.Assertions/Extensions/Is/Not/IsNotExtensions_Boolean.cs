﻿#nullable disable

using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static InvokableAssertionBuilder<bool, TAnd, TOr> IsNotTrue<TAnd, TOr>(this IValueSource<bool, TAnd, TOr> valueSource)
        where TAnd : IAnd<bool, TAnd, TOr>
        where TOr : IOr<bool, TAnd, TOr>
    {
        return new EqualsAssertCondition<bool>(false)
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<bool, TAnd, TOr> IsNotFalse<TAnd, TOr>(this IValueSource<bool, TAnd, TOr> valueSource)
        where TAnd : IAnd<bool, TAnd, TOr>
        where TOr : IOr<bool, TAnd, TOr>
    {
        return new EqualsAssertCondition<bool>(true)
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
}