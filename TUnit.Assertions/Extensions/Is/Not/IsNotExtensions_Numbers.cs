#nullable disable

using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions.Is.Not;

public static partial class IsNotExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> Zero<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot)
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new NotEqualsAssertCondition<TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), TActual.Zero));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> GreaterThan<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return value <= expected;
            },
            (value, _) => $"{value} was greater than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> GreaterThanOrEqualTo<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value < expected;
            },
            (value, _) => $"{value} was greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> LessThan<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value >= expected;
            },
            (value, _) => $"{value} was less than {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> LessThanOrEqualTo<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value > expected;
            },
            (value, _) => $"{value} was less than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Even<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value % 2 != 0;
            },
            (value, _) => $"{value} was even"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Odd<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot) 
        where TActual : INumber<TActual>, IModulusOperators<TActual, int, int>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value % 2 == 0;
            },
            (value, _) => $"{value} was odd"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Negative<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot) 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value >= TActual.Zero;
            },
            (value, _) => $"{value} was negative"));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Positive<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot) 
        where TActual : INumber<TActual>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TActual, TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), default, (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => $"{isNot.AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return value <= TActual.Zero;
            },
            (value, _) => $"{value} was positive"));
    }
}