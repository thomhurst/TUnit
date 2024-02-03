using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;

namespace TUnit.Assertions;

public class EnumerableCount<T, TInner> where T : IEnumerable<TInner> 
{
    protected AssertionBuilder<T> AssertionBuilder { get; }

    public EnumerableCount(AssertionBuilder<T> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public AssertCondition<T, int> EqualTo()
    {
        return new EnumerableCountEqualToAssertCondition<T, TInner>(AssertionBuilder, 0);
    }
}