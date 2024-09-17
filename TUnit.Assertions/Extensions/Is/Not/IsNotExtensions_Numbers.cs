#nullable disable

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotZero<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NotEqualsAssertCondition<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), TActual.Zero)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotGreaterThan<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value <= expected;
            },
            (value, _) => $"{value} was greater than {expected}")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotGreaterThanOrEqualTo<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value < expected;
            },
            (value, _) => $"{value} was greater than or equal to {expected}")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotLessThan<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value >= expected;
            },
            (value, _) => $"{value} was less than {expected}")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotLessThanOrEqualTo<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value > expected;
            },
            (value, _) => $"{value} was less than or equal to {expected}")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotEven<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value % 2 != 0;
            },
            (value, _) => $"{value} was even")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotOdd<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value % 2 == 0;
            },
            (value, _) => $"{value} was odd")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotNegative<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value >= TActual.Zero;
            },
            (value, _) => $"{value} was negative")
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotPositive<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{assertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value <= TActual.Zero;
            },
            (value, _) => $"{value} was positive")
            .ChainedTo(assertionBuilder);
    }
}