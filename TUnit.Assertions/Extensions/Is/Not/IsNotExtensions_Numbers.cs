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
    public static TOutput IsNotZero<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new NotEqualsAssertCondition<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), TActual.Zero)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotGreaterThan<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotGreaterThanOrEqualTo<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotLessThan<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotLessThanOrEqualTo<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotEven<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotOdd<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotNegative<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNotPositive<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
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
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
}