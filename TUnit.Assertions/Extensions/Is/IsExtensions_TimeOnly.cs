#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static TOutput IsGreaterThan<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeOnly, TAnd, TOr>, IOutputsChain<TOutput, TimeOnly>
        where TOutput : InvokableAssertionBuilder<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsGreaterThanOrEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeOnly, TAnd, TOr>, IOutputsChain<TOutput, TimeOnly>
        where TOutput : InvokableAssertionBuilder<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsLessThan<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeOnly, TAnd, TOr>, IOutputsChain<TOutput, TimeOnly>
        where TOutput : InvokableAssertionBuilder<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsLessThanOrEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeOnly, TAnd, TOr>, IOutputsChain<TOutput, TimeOnly>
        where TOutput : InvokableAssertionBuilder<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsBetween<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeOnly lowerBound, TimeOnly upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeOnly, TAnd, TOr>, IOutputsChain<TOutput, TimeOnly>
        where TOutput : InvokableAssertionBuilder<TimeOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) => value >= lowerBound && value <= upperBound,
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsEqualToWithTolerance<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TimeOnly expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<TimeOnly, TAnd, TOr>
        where TOr : IOr<TimeOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TimeOnly, TAnd, TOr>, IOutputsChain<TOutput, TimeOnly>
        where TOutput : InvokableAssertionBuilder<TimeOnly, TAnd, TOr>
    {
        return IsBetween<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected.Add(-tolerance), expected.Add(tolerance), doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}