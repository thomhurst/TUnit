#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions.Is;

public static partial class IsExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> EquivalentTo<TActual, TInner, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, IEnumerable<TInner> expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new EnumerableEquivalentToAssertCondition<TActual, TInner, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected));
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> Empty<TActual, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is)
        where TActual : IEnumerable
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return @is.Wrap(new EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr>(@is.AssertionBuilder.AppendCallerMethod(null), 0));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> Empty<TAnd, TOr>(this Is<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) =>
            {
                if (value is null)
                {
                    self.WithMessage((_, _) => "Actual string is null");
                    return false;
                }
                
                return value == string.Empty;
            },
            (s, _) => $"'{s}' was not empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> NullOrEmpty<TAnd, TOr>(this Is<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) => string.IsNullOrEmpty(value),
            (s, _) => $"'{s}' is not null or empty"));
    }
    
    public static BaseAssertCondition<string, TAnd, TOr> NullOrWhitespace<TAnd, TOr>(this Is<string, TAnd, TOr> @is)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return @is.Wrap(new DelegateAssertCondition<string, int,TAnd,TOr>(
            @is.AssertionBuilder.AppendCallerMethod(null), 0,
            (value, _, _, self) => string.IsNullOrWhiteSpace(value),
            (s, _) => $"'{s}' is not null or whitespace"));
    }
}