﻿#nullable disable

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions.Numbers;

public static class NumberIsExtensions
{
    public static NumberEqualToAssertionBuilderWrapper<TActual> IsEqualTo<TActual>(
        this IValueSource<TActual> valueSource, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TActual : INumber<TActual>
    {
        var assertionBuilder = valueSource.RegisterAssertion(new NumericEqualsAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue1]);
        
        return new NumberEqualToAssertionBuilderWrapper<TActual>(assertionBuilder);
    }

    public static InvokableValueAssertionBuilder<TActual> IsZero<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new EqualsAssertCondition<TActual>(TActual.Zero)
            , []);
    }

    public static InvokableValueAssertionBuilder<TActual> IsGreaterThan<TActual>(
        this IValueSource<TActual> valueSource, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(expected, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value > expected;
                },
                (value, _, _) => $"{value} was not greater than {expected}")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsGreaterThanOrEqualTo<TActual>(
        this IValueSource<TActual> valueSource, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value >= expected;
                },
                (value, _, _) => $"{value} was not greater than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsLessThan<TActual>(
        this IValueSource<TActual> valueSource, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value < expected;
                },
                (value, _, _) => $"{value} was not less than {expected}")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsLessThanOrEqualTo<TActual>(
        this IValueSource<TActual> valueSource, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value <= expected;
                },
                (value, _, _) => $"{value} was not less than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsDivisibleBy<TActual>(
        this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % expected == TActual.Zero;
                },
                (value, _, _) => $"{value} was not divisible by {expected}")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsEven<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % (TActual.One + TActual.One) == TActual.Zero;
                },
                (value, _, _) => $"{value} was not even")
            , []);
    }

    public static InvokableValueAssertionBuilder<TActual> IsOdd<TActual>(this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % (TActual.One + TActual.One) != TActual.Zero;
                },
                (value, _, _) => $"{value} was not odd")
            , []);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNegative<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value < TActual.Zero;
                },
                (value, _, _) => $"{value} was not negative")
            , []);
    }

    public static InvokableValueAssertionBuilder<TActual> IsPositive<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value > TActual.Zero;
                },
                (value, _, _) => $"{value} was not positive")
            , []);
    }
}