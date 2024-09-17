#nullable disable

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotZero<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NotEqualsAssertCondition<TActual, TAnd, TOr>(TActual.Zero)
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotGreaterThan<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _, actualExpression) => $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value <= expected;
            },
            (value, _, _) => $"{value} was greater than {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotGreaterThanOrEqualTo<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _, actualExpression) => $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value < expected;
            },
            (value, _, _) => $"{value} was greater than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotLessThan<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _, actualExpression) => $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value >= expected;
            },
            (value, _, _) => $"{value} was less than {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotLessThanOrEqualTo<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _, actualExpression) => $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value > expected;
            },
            (value, _, _) => $"{value} was less than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotEven<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _, actualExpression) => $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value % 2 != 0;
            },
            (value, _, _) => $"{value} was even")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotOdd<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _, actualExpression) => $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value % 2 == 0;
            },
            (value, _, _) => $"{value} was odd")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotNegative<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _, actualExpression) => $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value >= TActual.Zero;
            },
            (value, _, _) => $"{value} was negative")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsNotPositive<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _, actualExpression) => $"{valueSource.AssertionBuilder.ActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value <= TActual.Zero;
            },
            (value, _, _) => $"{value} was positive")
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
}