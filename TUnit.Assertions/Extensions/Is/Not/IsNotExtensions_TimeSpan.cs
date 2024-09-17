#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsNotZero<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new NotEqualsAssertCondition<TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), TimeSpan.Zero)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsNotGreaterThan<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) => value <= expected,
            (value, _) => $"{value} was greater than {expected}")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsNotGreaterThanOrEqualTo<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was greater than or equal to {expected}")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsNotLessThan<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was less than {expected}")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TimeSpan, TAnd, TOr> IsNotLessThanOrEqualTo<TAnd, TOr>(this AssertionBuilder<TimeSpan, TAnd, TOr> assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was less than or equal to {expected}")
            .ChainedTo(assertionBuilder);
    }
}