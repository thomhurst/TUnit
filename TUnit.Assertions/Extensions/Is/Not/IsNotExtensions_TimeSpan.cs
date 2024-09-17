#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsNotZero<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource)
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new NotEqualsAssertCondition<TimeSpan, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(null), TimeSpan.Zero)
            .ChainedTo(valueSource.AssertionBuilder);
    }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsNotGreaterThan<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) => value <= expected,
            (value, _) => $"{value} was greater than {expected}")
            .ChainedTo(valueSource.AssertionBuilder);
    }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsNotGreaterThanOrEqualTo<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was greater than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder);
    }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsNotLessThan<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was less than {expected}")
            .ChainedTo(valueSource.AssertionBuilder);
    }
    
    public static InvokableAssertionBuilder<TimeSpan, TAnd, TOr> IsNotLessThanOrEqualTo<TAnd, TOr>(this IValueSource<TimeSpan, TAnd, TOr> valueSource, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was less than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder);
    }
}