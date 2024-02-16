#nullable disable

using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class IsExtensions
{
    #region Strings
    
    public static BaseAssertCondition<string, TAnd, TOr> EqualTo<TAnd, TOr>(this Is<string, TAnd, TOr> @is, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("stringComparison")] string expression2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return @is.Wrap(new StringEqualsAssertCondition<TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), expected, stringComparison));
    }

    #endregion
    
    #region Numbers
    
    public static BaseAssertCondition<TActual, TAnd, TOr> EqualToWithTolerance<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, TActual tolerance, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("tolerance")] string expression2 = "")
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual,TActual,TAnd,TOr>(
            @is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), 
            expected,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                ArgumentNullException.ThrowIfNull(expected);
                
                return actual <= expected + tolerance && actual >= expected - tolerance;
            },
            (number, _) => $"{number} is not between {number! - tolerance} and {number! + tolerance}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Zero<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is)
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), TActual.Zero));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> GreaterThan<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string expectedExpression = "") where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> GreaterThanOrEqualTo<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> LessThan<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> LessThanOrEqualTo<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Even<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value % 2 == 0;
            },
            (value, _) => $"{value} was not even"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Odd<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value % 2 != 0;
            },
            (value, _) => $"{value} was not odd"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Negative<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is) 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value < TActual.Zero;
            },
            (value, _) => $"{value} was not negative"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Positive<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is) 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > TActual.Zero;
            },
            (value, _) => $"{value} was not positive"));
    }
    
    #endregion
    
    #region TimeSpans
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("tolerance")] string expression2 = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan,TimeSpan,TAnd,TOr>(
            @is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), 
            expected,
            (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                ArgumentNullException.ThrowIfNull(expected);
                
                return actual <= expected.Add(tolerance) && actual >= expected.Subtract(tolerance);
            },
            (timeSpan, _) => $"{timeSpan} is not between {timeSpan.Subtract(tolerance)} and {timeSpan.Add(tolerance)}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> Zero<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is)
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), TimeSpan.Zero));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThan<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThan<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
    
    #endregion

    #region DateTimes
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> Between<TAnd, TOr>(this Is<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset lowerBound, DateTimeOffset upperBound, [CallerArgumentExpression("lowerBound")] string expression1 = "", [CallerArgumentExpression("upperBound")] string expression2 = "")
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<TAnd, DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<TOr, DateTimeOffset, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not between {lowerBound.ToLongStringWithMilliseconds()} and {upperBound.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> Between<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime lowerBound, DateTime upperBound, [CallerArgumentExpression("lowerBound")] string expression1 = "", [CallerArgumentExpression("upperBound")] string expression2 = "")
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<TAnd, DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<TOr, DateTime, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not between {lowerBound.ToLongStringWithMilliseconds()} and {upperBound.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> GreaterThan<TAnd, TOr>(this Is<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<TAnd, DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<TOr, DateTimeOffset, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this Is<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<TAnd, DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<TOr, DateTimeOffset, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> LessThan<TAnd, TOr>(this Is<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<TAnd, DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<TOr, DateTimeOffset, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value < expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this Is<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<TAnd, DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<TOr, DateTimeOffset, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTimeOffset, DateTimeOffset, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value <= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> GreaterThan<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<TAnd, DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<TOr, DateTime, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<TAnd, DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<TOr, DateTime, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> LessThan<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<TAnd, DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<TOr, DateTime, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value < expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateTime, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<TAnd, DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<TOr, DateTime, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTime, DateTime, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value <= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> GreaterThan<TAnd, TOr>(this Is<DateOnly, TAnd, TOr> @is, DateOnly expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<TAnd, DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<TOr, DateOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this Is<DateOnly, TAnd, TOr> @is, DateOnly expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<TAnd, DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<TOr, DateOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> LessThan<TAnd, TOr>(this Is<DateOnly, TAnd, TOr> @is, DateOnly expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<TAnd, DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<TOr, DateOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this Is<DateOnly, TAnd, TOr> @is, DateOnly expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<TAnd, DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<TOr, DateOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> GreaterThan<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not greater than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> LessThan<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value < expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than {expected.ToLongStringWithMilliseconds()}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, [CallerArgumentExpression("expected")] string expectedExpression = "") 
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value <= expected;
            },
            (value, _) => $"{value.ToLongStringWithMilliseconds()} was not less than or equal to {expected.ToLongStringWithMilliseconds()}"));
    }
    
    #endregion
    
    #region Enumerables

    public static BaseAssertCondition<TActual, TAnd, TOr> EquivalentTo<TActual, TInner, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, IEnumerable<TInner> expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
        where TActual : IEnumerable<TInner>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new EnumerableEquivalentToAssertCondition<TActual, TInner, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(expectedExpression), expected));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Empty<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is)
        where TActual : IEnumerable
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), 0));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Empty<TAnd, TOr>(this Is<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);
                return value == string.Empty;
            },
            (s, _) => $"'{s}' was not empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> NullOrEmpty<TAnd, TOr>(this Is<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _) => string.IsNullOrEmpty(value),
            (s, _) => $"'{s}' is not null or empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> NullOrWhitespace<TAnd, TOr>(this Is<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _) => string.IsNullOrWhiteSpace(value),
            (s, _) => $"'{s}' is not null or whitespace"));
    }

    #endregion

    #region Booleans

    public static BaseAssertCondition<bool, TAnd, TOr> True<TAnd, TOr>(this Is<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), true));
    }
    
    public static BaseAssertCondition<bool, TAnd, TOr> False<TAnd, TOr>(this Is<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), false));
    }

    #endregion

    #region DateTimes
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> Between<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan lowerBound, TimeSpan upperBound, [CallerArgumentExpression("lowerBound")] string expression1 = "", [CallerArgumentExpression("upperBound")] string expression2 = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);
                ArgumentNullException.ThrowIfNull(lowerBound);
                ArgumentNullException.ThrowIfNull(upperBound);

                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> Between<TAnd, TOr>(this Is<DateOnly, TAnd, TOr> @is, DateOnly lowerBound, DateOnly upperBound, [CallerArgumentExpression("lowerBound")] string expression1 = "", [CallerArgumentExpression("upperBound")] string expression2 = "")
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<TAnd, DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<TOr, DateOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateOnly, DateOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);
                ArgumentNullException.ThrowIfNull(lowerBound);
                ArgumentNullException.ThrowIfNull(upperBound);

                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> Between<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly lowerBound, TimeOnly upperBound, [CallerArgumentExpression("lowerBound")] string expression1 = "", [CallerArgumentExpression("upperBound")] string expression2 = "")
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeOnly, TimeOnly, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);
                ArgumentNullException.ThrowIfNull(lowerBound);
                ArgumentNullException.ThrowIfNull(upperBound);

                return value >= lowerBound && value <= upperBound;
            },
            (value, _) => $"{value} was not between {lowerBound} and {upperBound}"));
    }

    public static BaseAssertCondition<DateTime, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("tolerance")] string expression2 = "")
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<TAnd, DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<TOr, DateTime, TAnd, TOr>
    {
        return Between(@is, expected - tolerance, expected + tolerance, expression1, expression2);
    }
    
    public static BaseAssertCondition<DateTimeOffset, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<DateTimeOffset, TAnd, TOr> @is, DateTimeOffset expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("tolerance")] string expression2 = "")
        where TAnd : And<DateTimeOffset, TAnd, TOr>, IAnd<TAnd, DateTimeOffset, TAnd, TOr>
        where TOr : Or<DateTimeOffset, TAnd, TOr>, IOr<TOr, DateTimeOffset, TAnd, TOr>
    {
        return Between(@is, expected - tolerance, expected + tolerance, expression1, expression2);
    }
    
    public static BaseAssertCondition<DateOnly, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<DateOnly, TAnd, TOr> @is, DateOnly expected, int daysTolerance, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("daysTolerance")] string expression2 = "")
        where TAnd : And<DateOnly, TAnd, TOr>, IAnd<TAnd, DateOnly, TAnd, TOr>
        where TOr : Or<DateOnly, TAnd, TOr>, IOr<TOr, DateOnly, TAnd, TOr>
    {
        return Between(@is, expected.AddDays(-daysTolerance), expected.AddDays(daysTolerance), expression1, expression2);
    }
    
    public static BaseAssertCondition<TimeOnly, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<TimeOnly, TAnd, TOr> @is, TimeOnly expected, TimeSpan tolerance, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("tolerance")] string expression2 = "")
        where TAnd : And<TimeOnly, TAnd, TOr>, IAnd<TAnd, TimeOnly, TAnd, TOr>
        where TOr : Or<TimeOnly, TAnd, TOr>, IOr<TOr, TimeOnly, TAnd, TOr>
    {
        return Between(@is, expected.Add(-tolerance), expected.Add(tolerance), expression1, expression2);
    }

    #endregion
}