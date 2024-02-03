using System.Collections;

namespace TUnit.Assertions;

public static class HasExtensions
{
    public static EnumerableCount<T> Count<T>(this Has<T> has) where T : IEnumerable
    {
        return new EnumerableCount<T>(has.AssertionBuilder, has.ConnectorType, has.OtherAssertCondition);
    }
    
    public static StringLength Length(this Has<string> has)
    {
        return new StringLength(has.AssertionBuilder, has.ConnectorType, has.OtherAssertCondition);
    }
}