#nullable disable

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions;

public static class NumberIsNotExtensions
{
    public static NumberNotEqualToAssertionBuilderWrapper<TActual> IsNotEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
    {
        var assertionBuilder = valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);
        
        return new NumberNotEqualToAssertionBuilderWrapper<TActual>(assertionBuilder);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotZero<TActual>(this IValueSource<TActual> valueSource)
        where TActual : INumber<TActual>
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(TActual.Zero)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotDivisibleBy<TActual>(
        this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
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
                (value, _, _) => $"{value} was not divisible by {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotGreaterThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
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
            (value, _, _) => $"{value} was greater than {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotGreaterThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
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
            (value, _, _) => $"{value} was greater than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotLessThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
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
            (value, _, _) => $"{value} was less than {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotLessThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
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
            (value, _, _) => $"{value} was less than or equal to {expected}")
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
            (value, _, _) => $"{value} was even")
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
            (value, _, _) => $"{value} was odd")
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
            (value, _, _) => $"{value} was negative")
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
            (value, _, _) => $"{value} was positive")
            , []);
    }
}