#nullable disable

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TActual> IsNotZero<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new NotEqualsAssertCondition<TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), TActual.Zero));
    }
    
    public static BaseAssertCondition<TActual> IsNotGreaterThan<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value <= expected;
            },
            (value, _) => $"{value} was greater than {expected}"));
    }
    
    public static BaseAssertCondition<TActual> IsNotGreaterThanOrEqualTo<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value < expected;
            },
            (value, _) => $"{value} was greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual> IsNotLessThan<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value >= expected;
            },
            (value, _) => $"{value} was less than {expected}"));
    }
    
    public static BaseAssertCondition<TActual> IsNotLessThanOrEqualTo<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value > expected;
            },
            (value, _) => $"{value} was less than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual> IsNotEven<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value % 2 != 0;
            },
            (value, _) => $"{value} was even"));
    }
    
    public static BaseAssertCondition<TActual> IsNotOdd<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value % 2 == 0;
            },
            (value, _) => $"{value} was odd"));
    }
    
    public static BaseAssertCondition<TActual> IsNotNegative<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value >= TActual.Zero;
            },
            (value, _) => $"{value} was negative"));
    }
    
    public static BaseAssertCondition<TActual> IsNotPositive<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> isNot) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(isNot.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value <= TActual.Zero;
            },
            (value, _) => $"{value} was positive"));
    }
}