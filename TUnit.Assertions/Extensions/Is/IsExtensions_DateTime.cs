#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static TOutput IsBetween<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateTime lowerBound, DateTime upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<DateTime, TAnd, TOr>
        where TOr : IOr<DateTime, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateTime, TAnd, TOr>, IOutputsChain<TOutput, DateTime>
        where TOutput : InvokableAssertionBuilder<DateTime, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not between {lowerBound.ToLongStringWithMilliseconds()} and {upperBound.ToLongStringWithMilliseconds()}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsGreaterThan<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<DateTime, TAnd, TOr>
        where TOr : IOr<DateTime, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateTime, TAnd, TOr>, IOutputsChain<TOutput, DateTime>
        where TOutput : InvokableAssertionBuilder<DateTime, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsGreaterThanOrEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateTime, TAnd, TOr>
        where TOr : IOr<DateTime, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateTime, TAnd, TOr>, IOutputsChain<TOutput, DateTime>
        where TOutput : InvokableAssertionBuilder<DateTime, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsLessThan<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateTime, TAnd, TOr>
        where TOr : IOr<DateTime, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateTime, TAnd, TOr>, IOutputsChain<TOutput, DateTime>
        where TOutput : InvokableAssertionBuilder<DateTime, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsLessThanOrEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateTime expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateTime, TAnd, TOr>
        where TOr : IOr<DateTime, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateTime, TAnd, TOr>, IOutputsChain<TOutput, DateTime>
        where TOutput : InvokableAssertionBuilder<DateTime, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsEqualToWithTolerance<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateTime expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<DateTime, TAnd, TOr>
        where TOr : IOr<DateTime, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateTime, TAnd, TOr>, IOutputsChain<TOutput, DateTime>
        where TOutput : InvokableAssertionBuilder<DateTime, TAnd, TOr>
    {
        return IsBetween<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected - tolerance, expected + tolerance, doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}