#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions.Is;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> GreaterThan<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IComparable<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value.CompareTo(expected) > 0;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> GreaterThanOrEqualTo<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value.CompareTo(expected) >= 0;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> LessThan<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value.CompareTo(expected) < 0;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> LessThanOrEqualTo<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value.CompareTo(expected) <= 0;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Between<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual lowerBound, TActual upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TActual : IComparable<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, self) =>
            {
                return value.CompareTo(lowerBound) >= 0 && value.CompareTo(upperBound) <= 0;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }
}