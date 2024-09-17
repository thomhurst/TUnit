﻿#nullable disable

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Numbers;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsEqualToWithTolerance<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, TActual tolerance,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "",
        [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(expected,
                (actual, _, _, _) =>
                {
                    ArgumentNullException.ThrowIfNull(actual);
                    ArgumentNullException.ThrowIfNull(expected);

                    return actual <= expected + tolerance && actual >= expected - tolerance;
                },
                (number, _, _) => $"{number} is not between {number! - tolerance} and {number! + tolerance}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsZero<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new EqualsAssertCondition<TActual>(TActual.Zero)
            .ChainedTo(valueSource.AssertionBuilder, []);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsGreaterThan<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(expected, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{self.RawActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value > expected;
                },
                (value, _, _) => $"{value} was not greater than {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsGreaterThanOrEqualTo<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value >= expected;
                },
                (value, _, _) => $"{value} was not greater than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsLessThan<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value < expected;
                },
                (value, _, _) => $"{value} was not less than {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsLessThanOrEqualTo<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value <= expected;
                },
                (value, _, _) => $"{value} was not less than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsDivisibleBy<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % expected == TActual.Zero;
                },
                (value, _, _) => $"{value} was not divisible by {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsEven<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource)
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % (TActual.One + TActual.One) == TActual.Zero;
                },
                (value, _, _) => $"{value} was not even")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsOdd<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource)
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % (TActual.One + TActual.One) != TActual.Zero;
                },
                (value, _, _) => $"{value} was not odd")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNegative<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value < TActual.Zero;
                },
                (value, _, _) => $"{value} was not negative")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }

    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsPositive<TActual, TAnd, TOr>(
        this IValueSource<TActual, TAnd, TOr> valueSource)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value > TActual.Zero;
                },
                (value, _, _) => $"{value} was not positive")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
}