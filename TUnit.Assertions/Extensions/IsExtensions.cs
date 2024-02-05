using System.Numerics;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class IsExtensions
{
    #region Strings

    public static BaseAssertCondition<string, TAnd, TOr> EqualTo<TAnd, TOr>(this Is<string, TAnd, TOr> @is, string expected)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return EqualTo(@is, expected, StringComparison.Ordinal);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> EqualTo<TAnd, TOr>(this Is<string, TAnd, TOr> @is, string expected, StringComparison stringComparison)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return @is.Wrap(new StringEqualsAssertCondition<TAnd, TOr>(@is.AssertionBuilder, expected, stringComparison));
    }

    #endregion
    
    #region Numbers
    
    public static BaseAssertCondition<TActual, TAnd, TOr> EqualToWithTolerance<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected, TActual tolerance)
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual,TActual,TAnd,TOr>(
            @is.AssertionBuilder, 
            expected,
            (actual, expected, _) =>
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
        return @is.Wrap(new EqualsAssertCondition<TActual, TAnd, TOr>(@is.AssertionBuilder, TActual.Zero));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> GreaterThan<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected) where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> GreaterThanOrEqualTo<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected) 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> LessThan<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected) 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> LessThanOrEqualTo<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TActual expected) 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
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
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
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
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
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
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
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
        return @is.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > TActual.Zero;
            },
            (value, _) => $"{value} was not positive"));
    }
    
    #endregion
    
    #region TimeSpans
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected, TimeSpan tolerance)
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan,TimeSpan,TAnd,TOr>(
            @is.AssertionBuilder, 
            expected,
            (actual, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                ArgumentNullException.ThrowIfNull(expected);
                
                return actual <= expected + tolerance && actual >= expected - tolerance;
            },
            (timeSpan, _) => $"{timeSpan} is not between {timeSpan - tolerance} and {timeSpan + tolerance}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> Zero<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is)
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<TimeSpan, TAnd, TOr>(@is.AssertionBuilder, TimeSpan.Zero));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThan<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected)
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected) 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThan<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected) 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this Is<TimeSpan, TAnd, TOr> @is, TimeSpan expected) 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(@is.AssertionBuilder, default, (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);

                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
    
    #endregion

    #region Enumerables

    public static BaseAssertCondition<TActual, TAnd, TOr> EquivalentTo<TActual, TInner, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, IEnumerable<TInner?> expected)
        where TActual : IEnumerable<TInner?>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new EnumerableEquivalentToAssertCondition<TActual, TInner?, TAnd, TOr>(@is.AssertionBuilder, expected));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Empty<TActual, TInner, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is)
        where TActual : IEnumerable<TInner>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new EnumerableCountEqualToAssertCondition<TActual, TInner, TAnd, TOr>(@is.AssertionBuilder, 0));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Empty<TAnd, TOr>(this Is<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionBuilder, 0,
            (value, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(value);
                return value == string.Empty;
            },
            (s, _) => $"'{s}' was not empty"));
    }

    #endregion

    #region Booleans

    public static BaseAssertCondition<bool, TAnd, TOr> True<TAnd, TOr>(this Is<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionBuilder, true));
    }
    
    public static BaseAssertCondition<bool, TAnd, TOr> False<TAnd, TOr>(this Is<bool, TAnd, TOr> @is)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return @is.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(@is.AssertionBuilder, false));
    }

    #endregion

    #region DateTimes

    public static BaseAssertCondition<DateTime, TAnd, TOr> EqualToWithTolerance<TAnd, TOr>(this Is<DateTime, TAnd, TOr> @is, DateTime expected, TimeSpan tolerance)
        where TAnd : And<DateTime, TAnd, TOr>, IAnd<TAnd, DateTime, TAnd, TOr>
        where TOr : Or<DateTime, TAnd, TOr>, IOr<TOr, DateTime, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<DateTime,DateTime,TAnd,TOr>(
            @is.AssertionBuilder, 
            expected,
            (actual, expected, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                ArgumentNullException.ThrowIfNull(expected);
                
                return actual <= expected + tolerance && actual >= expected - tolerance;
            },
            (dateTime, _) => $"{dateTime} is not between {dateTime - tolerance} and {dateTime + tolerance}"));
    }

    #endregion
}