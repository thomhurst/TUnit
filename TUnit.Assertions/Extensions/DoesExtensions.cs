using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class DoesExtensions
{
    public static AssertCondition<T, TInner> Contain<T, TInner>(this Is<T> @is, TInner expected)
        where T : IEnumerable<TInner>
    {
        return new EnumerableContainsAssertCondition<T, TInner>(@is.AssertionBuilder, expected);
    }
    
    public static AssertCondition<string, string> Contain(this Is<string> @is, string expected)
    {
        return Contain(@is, expected, StringComparison.Ordinal);
    }
    
    public static AssertCondition<string, string> Contain(this Is<string> @is, string expected, StringComparison stringComparison)
    {
        return new StringContainsAssertCondition(@is.AssertionBuilder, expected, stringComparison);
    }
}