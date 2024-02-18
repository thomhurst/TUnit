#nullable disable

using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class IsNotExtensions
{
    #region Strings

    public static BaseAssertCondition<string, TAnd, TOr> EqualTo<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot, string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return EqualTo(isNot, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> EqualTo<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot, string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return isNot.Wrap(new StringNotEqualsAssertCondition<TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison));
    }

    #endregion
    
    #region Numbers
    
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
    
    #endregion
    
    #region TimeSpans
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> Zero<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot)
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new NotEqualsAssertCondition<TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), TimeSpan.Zero));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThan<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) => value <= expected,
            (value, _) => $"{value} was greater than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> GreaterThanOrEqualTo<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value < expected;
            },
            (value, _) => $"{value} was greater than or equal to {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThan<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value >= expected;
            },
            (value, _) => $"{value} was less than {expected}"));
    }
    
    public static BaseAssertCondition<TimeSpan, TAnd, TOr> LessThanOrEqualTo<TAnd, TOr>(this IsNot<TimeSpan, TAnd, TOr> isNot, TimeSpan expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TAnd : And<TimeSpan, TAnd, TOr>, IAnd<TAnd, TimeSpan, TAnd, TOr>
        where TOr : Or<TimeSpan, TAnd, TOr>, IOr<TOr, TimeSpan, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<TimeSpan, TimeSpan, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), default, (value, _, _, self) =>
            {
                return value > expected;
            },
            (value, _) => $"{value} was less than or equal to {expected}"));
    }
    
    #endregion

    #region Enumerables

    public static BaseAssertCondition<TActual, TAnd, TOr> EquivalentTo<TActual, TInner, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot, IEnumerable<TInner> expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new EnumerableNotEquivalentToAssertCondition<TActual, TInner, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Empty<TActual, TAnd, TOr>(this IsNot<TActual, TAnd, TOr> isNot)
        where TActual : IEnumerable
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return isNot.Wrap(new EnumerableCountNotEqualToAssertCondition<TActual, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), 0));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Empty<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<string, int,TAnd,TOr>(
            isNot.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) => value != string.Empty,
            (s, _) => $"'{s}' is empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> NullOrEmpty<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<string, int, TAnd,TOr>(
            isNot.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) => !string.IsNullOrEmpty(value),
            (s, _) => $"'{s}' is null or empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> NullOrWhitespace<TAnd, TOr>(this IsNot<string, TAnd, TOr> isNot)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return isNot.Wrap(new DelegateAssertCondition<string, int,TAnd,TOr>(
            isNot.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) => !string.IsNullOrWhiteSpace(value),
            (s, _) => $"'{s}' is null or whitespace"));
    }

    #endregion

    #region Booleans

    public static BaseAssertCondition<bool, TAnd, TOr> True<TAnd, TOr>(this IsNot<bool, TAnd, TOr> isNot)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return isNot.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), false));
    }
    
    public static BaseAssertCondition<bool, TAnd, TOr> False<TAnd, TOr>(this IsNot<bool, TAnd, TOr> isNot)
        where TAnd : And<bool, TAnd, TOr>, IAnd<TAnd, bool, TAnd, TOr>
        where TOr : Or<bool, TAnd, TOr>, IOr<TOr, bool, TAnd, TOr>
    {
        return isNot.Wrap(new EqualsAssertCondition<bool, TAnd, TOr>(isNot.AssertionBuilder.AppendCallerMethod(null), true));
    }

    #endregion
}