#nullable disable

#if NET

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class NumberIsExtensions
{
    public static GenericEqualToAssertionBuilderWrapper<TActual> Within<TActual>(
        this GenericEqualToAssertionBuilderWrapper<TActual> assertionBuilder, TActual tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
        where TActual : INumber<TActual>
    {
        var assertion = assertionBuilder.GetLastAssertion() as EqualsExpectedValueAssertCondition<TActual>;
        if (assertion is null)
        {
            throw new InvalidOperationException($"Expected last assertion to be EqualsExpectedValueAssertCondition<{typeof(TActual).Name}>");
        }

        assertion.WithComparer((actual, expected) =>
        {
            if (actual.IsBetween(expected - tolerance, expected + tolerance))
            {
                return AssertionDecision.Pass;
            }
            
            return AssertionDecision.Fail($"Expected {actual} to be not equal to {expected} +={tolerance}.");
        });
        
        assertionBuilder.AppendCallerMethod([doNotPopulateThis]);
        
        return assertionBuilder;
    } 

    public static AssertionBuilder<TActual> IsZero<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(TActual.Zero)
            , []);
    }
    
    public static AssertionBuilder<TActual> IsDivisibleBy<TActual>(
        this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % expected == TActual.Zero;
                },
                (value, _, _) => $"{value} was not divisible by {expected}",
                $"to be divisible by {expected}")
            , [doNotPopulateThisValue]);
    }

    public static AssertionBuilder<TActual> IsEven<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % (TActual.One + TActual.One) == TActual.Zero;
                },
                (value, _, _) => $"{value} was not even",
                $"to be even")
            , []);
    }

    public static AssertionBuilder<TActual> IsOdd<TActual>(this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % (TActual.One + TActual.One) != TActual.Zero;
                },
                (value, _, _) => $"{value} was not odd",
                $"to be odd")
            , []);
    }

    public static AssertionBuilder<TActual> IsNegative<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value < TActual.Zero;
                },
                (value, _, _) => $"{value} was not negative",
                $"to be negative")
            , []);
    }

    public static AssertionBuilder<TActual> IsPositive<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value > TActual.Zero;
                },
                (value, _, _) => $"{value} was not positive",
                $"to be positive")
            , []);
    }
}

#endif
