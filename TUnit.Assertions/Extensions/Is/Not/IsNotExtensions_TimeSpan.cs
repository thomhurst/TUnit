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
    public static TOutput IsNotZero<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeSpan, TAnd, TOr>, IOutputsChain<TOutput, TimeSpan>
        where TOutput : InvokableAssertionBuilder<TimeSpan, TAnd, TOr>
    {
        return new NotEqualsAssertCondition<TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), TimeSpan.Zero)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotGreaterThan<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeSpan, TAnd, TOr>, IOutputsChain<TOutput, TimeSpan>
        where TOutput : InvokableAssertionBuilder<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) => value <= expected,
            (value, _) => $"{value} was greater than {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotGreaterThanOrEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeSpan, TAnd, TOr>, IOutputsChain<TOutput, TimeSpan>
        where TOutput : InvokableAssertionBuilder<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was greater than or equal to {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotLessThan<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeSpan, TAnd, TOr>, IOutputsChain<TOutput, TimeSpan>
        where TOutput : InvokableAssertionBuilder<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was less than {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotLessThanOrEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeSpan, TAnd, TOr>
        where TOr : IOr<TimeSpan, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeSpan, TAnd, TOr>, IOutputsChain<TOutput, TimeSpan>
        where TOutput : InvokableAssertionBuilder<TimeSpan, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was less than or equal to {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
}