#nullable disable

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
    public static TOutput IsEqualToWithTolerance<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(
        this TAssertionBuilder assertionBuilder, TActual expected, TActual tolerance,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "",
        [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(
                assertionBuilder.AppendCallerMethodWithMultipleExpressions([
                    doNotPopulateThisValue1, doNotPopulateThisValue2
                ]),
                expected,
                (actual, _, _, _) =>
                {
                    ArgumentNullException.ThrowIfNull(actual);
                    ArgumentNullException.ThrowIfNull(expected);

                    return actual <= expected + tolerance && actual >= expected - tolerance;
                },
                (number, _) => $"{number} is not between {number! - tolerance} and {number! + tolerance}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsZero<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(
        this TAssertionBuilder assertionBuilder)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new EqualsAssertCondition<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), TActual.Zero)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsGreaterThan<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(
        this TAssertionBuilder assertionBuilder, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(
                assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.WithMessage((_, _) =>
                            $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                        return false;
                    }

                    return value > expected;
                },
                (value, _) => $"{value} was not greater than {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsGreaterThanOrEqualTo<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(
        this TAssertionBuilder assertionBuilder, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(
                assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.WithMessage((_, _) =>
                            $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                        return false;
                    }

                    return value >= expected;
                },
                (value, _) => $"{value} was not greater than or equal to {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsLessThan<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(
        this TAssertionBuilder assertionBuilder, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(
                assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.WithMessage((_, _) =>
                            $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                        return false;
                    }

                    return value < expected;
                },
                (value, _) => $"{value} was not less than {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsLessThanOrEqualTo<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(
        this TAssertionBuilder assertionBuilder, TActual expected,
        [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(
                assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.WithMessage((_, _) =>
                            $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                        return false;
                    }

                    return value <= expected;
                },
                (value, _) => $"{value} was not less than or equal to {expected}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsEven<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(
        this TAssertionBuilder assertionBuilder)
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null),
                default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.WithMessage((_, _) =>
                            $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                        return false;
                    }

                    return value % 2 == 0;
                },
                (value, _) => $"{value} was not even")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsOdd<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null),
                default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.WithMessage((_, _) =>
                            $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                        return false;
                    }

                    return value % 2 != 0;
                },
                (value, _) => $"{value} was not odd")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsNegative<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(
        this TAssertionBuilder assertionBuilder)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null),
                default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.WithMessage((_, _) =>
                            $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                        return false;
                    }

                    return value < TActual.Zero;
                },
                (value, _) => $"{value} was not negative")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsPositive<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(
        this TAssertionBuilder assertionBuilder)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null),
                default, (value, _, _, self) =>
                {
                    if (value is null)
                    {
                        self.WithMessage((_, _) =>
                            $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                        return false;
                    }

                    return value > TActual.Zero;
                },
                (value, _) => $"{value} was not positive")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
}