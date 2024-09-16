#nullable disable

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions.Numbers;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TActual> IsEqualToWithTolerance<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, TActual tolerance, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("tolerance")] string doNotPopulateThisValue2 = "")
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<TActual,TActual,TAnd,TOr>(
            @is.AssertionConnector.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), 
            expected,
            (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                ArgumentNullException.ThrowIfNull(expected);
                
                return actual <= expected + tolerance && actual >= expected - tolerance;
            },
            (number, _) => $"{number} is not between {number! - tolerance} and {number! + tolerance}"));
    }
    
    public static BaseAssertCondition<TActual> IsZero<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is)
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new EqualsAssertCondition<TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), TActual.Zero));
    }
    
    public static BaseAssertCondition<TActual> IsGreaterThan<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{@is.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value > expected;
            },
            (value, _) => $"{value} was not greater than {expected}"));
    }
    
    public static BaseAssertCondition<TActual> IsGreaterThanOrEqualTo<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{@is.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value >= expected;
            },
            (value, _) => $"{value} was not greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual> IsLessThan<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{@is.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value < expected;
            },
            (value, _) => $"{value} was not less than {expected}"));
    }
    
    public static BaseAssertCondition<TActual> IsLessThanOrEqualTo<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{@is.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value <= expected;
            },
            (value, _) => $"{value} was not less than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual> IsEven<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{@is.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value % 2 == 0;
            },
            (value, _) => $"{value} was not even"));
    }
    
    public static BaseAssertCondition<TActual> IsOdd<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{@is.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value % 2 != 0;
            },
            (value, _) => $"{value} was not odd"));
    }
    
    public static BaseAssertCondition<TActual> IsNegative<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{@is.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value < TActual.Zero;
            },
            (value, _) => $"{value} was not negative"));
    }
    
    public static BaseAssertCondition<TActual> IsPositive<TActual, TAnd, TOr>(this IIs<TActual, TAnd, TOr> @is) 
        where TActual : INumber<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(@is.AssertionConnector, new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(@is.AssertionConnector.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{@is.AssertionConnector.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value > TActual.Zero;
            },
            (value, _) => $"{value} was not positive"));
    }
}