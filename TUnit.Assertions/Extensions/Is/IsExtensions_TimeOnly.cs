#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions.Is;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> GreaterThan<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value > expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> LessThan<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value < expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value <= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> Between<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly lowerBound, TimeOnly upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), default, (value, _, _, self) => value >= lowerBound && value <= upperBound,
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return Between(@is, expected.Add(-tolerance), expected.Add(tolerance), doNotPopulateThisValue1, doNotPopulateThisValue2);
    }
}