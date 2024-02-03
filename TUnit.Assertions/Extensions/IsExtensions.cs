using System.Numerics;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Numbers;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class IsExtensions
{
    #region Strings

    public static AssertCondition<string, string> EqualTo(this Is<string> @is, string expected)
    {
        return EqualTo(@is, expected, StringComparison.Ordinal);
    }
    
    public static AssertCondition<string, string> EqualTo(this Is<string> @is, string expected, StringComparison stringComparison)
    {
        return new StringEqualsAssertCondition(@is.AssertionBuilder, expected, stringComparison);
    }

    #endregion
    
    #region Numbers
    
    public static AssertCondition<TActual, TActual> Zero<TActual>(this Is<TActual> @is)
        where TActual : INumber<TActual>
    {
        return new EqualsAssertCondition<TActual, TActual>(@is.AssertionBuilder, TActual.Zero);
    }
    
    public static AssertCondition<T, T> GreaterThan<T>(this Is<T> @is, T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T, T>(@is.AssertionBuilder,  expected);
    }
    
    public static AssertCondition<T, T> GreaterThanOrEqualTo<T>(this Is<T> @is, T expected) where T : INumber<T>
    {
        return new GreaterThanOrEqualToAssertCondition<T, T>(@is.AssertionBuilder,  expected);
    }
    
    public static AssertCondition<T, T> LessThan<T>(this Is<T> @is, T expected) where T : INumber<T>
    {
        return new GreaterThanAssertCondition<T, T>(@is.AssertionBuilder,  expected);
    }
    
    public static AssertCondition<T, T> LessThanOrEqualTo<T>(this Is<T> @is, T expected) where T : INumber<T>
    {
        return new LessThanOrEqualToAssertCondition<T, T>(@is.AssertionBuilder,  expected);
    }
    
    public static AssertCondition<T, T> Even<T>(this Is<T> @is) where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new IsEvenAssertCondition<T>(@is.AssertionBuilder);
    }
    
    public static AssertCondition<T, T> Odd<T>(this Is<T> @is) where T : INumber<T>, IModulusOperators<T, int, int>
    {
        return new IsOddAssertCondition<T>(@is.AssertionBuilder);
    }
    
    #endregion

    #region Enumerables

    public static AssertCondition<T, IEnumerable<TInner>> EquivalentTo<T, TInner>(this Is<T> @is, IEnumerable<TInner> expected)
        where T : IEnumerable<TInner>
    {
        return new EnumerableEquivalentToAssertCondition<T, TInner>(@is.AssertionBuilder, expected);
    }
    
    public static AssertCondition<T, int> Empty<T, TInner>(this Is<T> @is)
        where T : IEnumerable<TInner>
    {
        return new EnumerableCountEqualToAssertCondition<T, TInner>(@is.AssertionBuilder, 0);
    }

    #endregion

    #region Booleans

    public static AssertCondition<bool, bool> True(this Is<bool> @is)
    {
        return new EqualsAssertCondition<bool, bool>(@is.AssertionBuilder, true);
    }
    
    public static AssertCondition<bool, bool> False(this Is<bool> @is)
    {
        return new EqualsAssertCondition<bool, bool>(@is.AssertionBuilder, false);
    }

    #endregion
}