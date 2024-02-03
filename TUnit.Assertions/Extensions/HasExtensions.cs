using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static class HasExtensions
{
    public static EnumerableCount<T, TInner> Count<T, TInner>(this Is<T> @is) where T : IEnumerable<TInner>
    {
        return new EnumerableCount<T, TInner>(@is.AssertionBuilder);
    }
    
    public static StringLength Length(this Is<string> @is)
    {
        return new StringLength(@is.AssertionBuilder);
    }
}