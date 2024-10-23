#nullable disable

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
        var assertion = (EqualsExpectedValueAssertCondition<TActual>) assertionBuilder.Assertions.Peek();

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

    public static InvokableValueAssertionBuilder<TActual> IsZero<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(TActual.Zero)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsDivisibleBy<TActual>(
        this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % expected == TActual.Zero;
                },
                (value, _, _) => $"{value} was not divisible by {expected}",
                $"to be divisible by {expected}")
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsEven<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % (TActual.One + TActual.One) == TActual.Zero;
                },
                (value, _, _) => $"{value} was not even",
                $"to be even")
            , []);
    }

    public static InvokableValueAssertionBuilder<TActual> IsOdd<TActual>(this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>, IModulusOperators<TActual, TActual, TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value % (TActual.One + TActual.One) != TActual.Zero;
                },
                (value, _, _) => $"{value} was not odd",
                $"to be odd")
            , []);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNegative<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value < TActual.Zero;
                },
                (value, _, _) => $"{value} was not negative",
                $"to be negative")
            , []);
    }

    public static InvokableValueAssertionBuilder<TActual> IsPositive<TActual>(
        this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
                {
                    if (value is null)
                    {
                        self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return value > TActual.Zero;
                },
                (value, _, _) => $"{value} was not positive",
                $"to be positive")
            , []);
    }
}