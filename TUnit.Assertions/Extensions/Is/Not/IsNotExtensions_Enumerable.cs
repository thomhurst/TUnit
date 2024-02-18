#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions.Is.Not;

public static partial class IsNotExtensions
{
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
}