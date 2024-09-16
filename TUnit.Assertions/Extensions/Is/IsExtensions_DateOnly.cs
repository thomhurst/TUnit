#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static TOutput IsGreaterThan<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateOnly, TAnd, TOr>, IOutputsChain<TOutput, DateOnly>
        where TOutput : InvokableAssertionBuilder<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsGreaterThanOrEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateOnly, TAnd, TOr>, IOutputsChain<TOutput, DateOnly>
        where TOutput : InvokableAssertionBuilder<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsLessThan<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateOnly, TAnd, TOr>, IOutputsChain<TOutput, DateOnly>
        where TOutput : InvokableAssertionBuilder<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsLessThanOrEqualTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateOnly, TAnd, TOr>, IOutputsChain<TOutput, DateOnly>
        where TOutput : InvokableAssertionBuilder<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsBetween<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateOnly lowerBound, DateOnly upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateOnly, TAnd, TOr>, IOutputsChain<TOutput, DateOnly>
        where TOutput : InvokableAssertionBuilder<DateOnly, TAnd, TOr>
    {
        return new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(assertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) =>
            {
                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
    
    public static TOutput IsEqualToWithTolerance<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, DateOnly expected, int daysTolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("daysTolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : IAnd<DateOnly, TAnd, TOr>
        where TOr : IOr<DateOnly, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<DateOnly, TAnd, TOr>, IOutputsChain<TOutput, DateOnly>
        where TOutput : InvokableAssertionBuilder<DateOnly, TAnd, TOr>
    {
        return IsBetween<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder, expected.AddDays(-daysTolerance), expected.AddDays(daysTolerance), doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}