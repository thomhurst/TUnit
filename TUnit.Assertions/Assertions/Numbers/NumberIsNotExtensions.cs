#nullable disable

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class NumberIsNotExtensions
{
    public static GenericNotEqualToAssertionBuilderWrapper<TActual> Within<TActual>(
        this GenericNotEqualToAssertionBuilderWrapper<TActual> assertionBuilder, TActual tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
        where TActual : INumber<TActual>
    {
        var assertion = (NotEqualsExpectedValueAssertCondition<TActual>) assertionBuilder.Assertions.Peek();

        assertion.WithComparer((actual, expected) =>
        {
            if (!actual.IsBetween(expected - tolerance, expected + tolerance))
            {
                return AssertionDecision.Pass;
            }
            
            return AssertionDecision.Fail($"Expected {actual} to be equal to {expected} +={tolerance}.");
        });
        
        assertionBuilder.AppendCallerMethod([doNotPopulateThis]);
        
        return assertionBuilder;
    } 
    
    public static InvokableValueAssertionBuilder<TActual> IsNotZero<TActual>(this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(TActual.Zero)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotDivisibleBy<TActual>(
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

                    return value % expected != TActual.Zero;
                },
                (value, _, _) => $"{value} was not divisible by {expected}",
                $"to not be divisible by {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotGreaterThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }

                return value <= expected;
            },
            (value, _, _) => $"{value} was greater than {expected}",
            $"to not be greater than {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotGreaterThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value < expected;
            },
            (value, _, _) => $"{value} was greater than or equal to {expected}",
            $"to not be greater than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotLessThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value >= expected;
            },
            (value, _, _) => $"{value} was less than {expected}",
            $"to not be less than {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotLessThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value > expected;
            },
            (value, _, _) => $"{value} was less than or equal to {expected}",
            $"to not be less than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotEven<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value % 2 != 0;
            },
            (value, _, _) => $"{value} was even",
            $"to not be even")
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotOdd<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value % 2 == 0;
            },
            (value, _, _) => $"{value} was odd",
            $"to not be odd")
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotNegative<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value >= TActual.Zero;
            },
            (value, _, _) => $"{value} was negative",
            $"to not be negative")
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotPositive<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value <= TActual.Zero;
            },
            (value, _, _) => $"{value} was positive",
            $"to not be positive")
            , []);
    }
}