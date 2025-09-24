#nullable disable

#if NET

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class NumberExtensions
{
    #region Positive Assertions
    
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
    
    #endregion
    
    #region Negative Assertions
    
    public static AssertionBuilder<TActual> IsNotZero<TActual>(this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(TActual.Zero)
            , []);
    }
    
    public static AssertionBuilder<TActual> IsNotDivisibleBy<TActual>(
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

                    return value % expected != TActual.Zero;
                },
                (value, _, _) => $"{value} was divisible by {expected}",
                $"to not be divisible by {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static AssertionBuilder<TActual> IsNotGreaterThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }

                return value <= expected;
            },
            (value, _, _) => $"{value} was greater than {expected}",
            $"to not be greater than {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static AssertionBuilder<TActual> IsNotGreaterThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value < expected;
            },
            (value, _, _) => $"{value} was greater than or equal to {expected}",
            $"to not be greater than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static AssertionBuilder<TActual> IsNotLessThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value >= expected;
            },
            (value, _, _) => $"{value} was less than {expected}",
            $"to not be less than {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static AssertionBuilder<TActual> IsNotLessThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null) 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value > expected;
            },
            (value, _, _) => $"{value} was less than or equal to {expected}",
            $"to not be less than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static AssertionBuilder<TActual> IsNotEven<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value % 2 != 0;
            },
            (value, _, _) => $"{value} was even",
            $"to not be even")
            , []);
    }
    
    public static AssertionBuilder<TActual> IsNotOdd<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value % 2 == 0;
            },
            (value, _, _) => $"{value} was odd",
            $"to not be odd")
            , []);
    }
    
    public static AssertionBuilder<TActual> IsNotNegative<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value >= TActual.Zero;
            },
            (value, _, _) => $"{value} was negative",
            $"to not be negative")
            , []);
    }
    
    public static AssertionBuilder<TActual> IsNotPositive<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default(TActual), (value, _, self) =>
            {
                if (value is null)
                {
                    self.OverriddenMessage = $"{valueSource.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return value <= TActual.Zero;
            },
            (value, _, _) => $"{value} was positive",
            $"to not be positive")
            , []);
    }
    
    #endregion
}

#endif
