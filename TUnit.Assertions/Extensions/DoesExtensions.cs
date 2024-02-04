using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class DoesExtensions
{
    public static AssertCondition<TActual, TInner, TAnd, TOr> Contain<TActual, TInner, TAnd, TOr>(this Is<TActual, TAnd, TOr> @is, TInner expected)
        where TActual : IEnumerable<TInner>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return new EnumerableContainsAssertCondition<TActual, TInner, TAnd, TOr>(@is.AssertionBuilder, expected);
    }
    
    public static AssertCondition<string, string, TAnd, TOr> Contain<TAnd, TOr>(this Is<string, TAnd, TOr> @is, string expected)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return Contain(@is, expected, StringComparison.Ordinal);
    }
    
    public static AssertCondition<string, string, TAnd, TOr> Contain<TAnd, TOr>(this Is<string, TAnd, TOr> @is, string expected, StringComparison stringComparison)
        where TAnd : And<string, TAnd, TOr>, IAnd<TAnd, string, TAnd, TOr>
        where TOr : Or<string, TAnd, TOr>, IOr<TOr, string, TAnd, TOr>
    {
        return new StringContainsAssertCondition<TAnd, TOr>(@is.AssertionBuilder, expected, stringComparison);
    }
}