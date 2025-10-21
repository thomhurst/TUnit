using System.Collections;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Assertion that evaluates the count of a collection and provides numeric assertions on that count.
/// Implements IAssertionSource&lt;int&gt; to enable all numeric assertion methods.
/// </summary>
public class CollectionCountValueAssertion<TCollection, TItem> : Sources.ValueAssertion<int>
    where TCollection : IEnumerable<TItem>
{
    public CollectionCountValueAssertion(
        AssertionContext<TCollection> collectionContext,
        Func<TItem, bool>? predicate)
        : base(CreateIntContext(collectionContext, predicate))
    {
    }

    private static AssertionContext<int> CreateIntContext(
        AssertionContext<TCollection> collectionContext,
        Func<TItem, bool>? predicate)
    {
        return collectionContext.Map<int>(collection =>
        {
            if (collection == null)
            {
                return 0;
            }

            // Calculate count efficiently
            if (predicate == null)
            {
                return collection switch
                {
                    ICollection c => c.Count,
                    _ => System.Linq.Enumerable.Count(collection)
                };
            }

            return System.Linq.Enumerable.Count(collection, predicate);
        });
    }
}
