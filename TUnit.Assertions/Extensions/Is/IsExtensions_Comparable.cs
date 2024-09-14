#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> IsGreaterThan<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) > 0;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> IsGreaterThanOrEqualTo<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) >= 0;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> IsLessThan<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) < 0;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> IsLessThanOrEqualTo<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) <= 0;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> IsBetween<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual lowerBound, TActual upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.Is(), new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.Is().AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, _) =>
            {
                return value.CompareTo(lowerBound) >= 0 && value.CompareTo(upperBound) <= 0;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }
}