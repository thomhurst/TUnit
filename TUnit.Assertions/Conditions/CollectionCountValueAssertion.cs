using System.Collections;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Assertion that evaluates the count of a collection and provides numeric assertions on that count.
/// Implements IAssertionSource&lt;int&gt; to enable all numeric assertion methods.
/// </summary>
public class CollectionCountValueAssertion<TCollection, TItem> : ValueAssertion<int>
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

/// <summary>
/// Assertion that evaluates the count of items satisfying an inner assertion and provides numeric assertions on that count.
/// Implements IAssertionSource&lt;int&gt; to enable all numeric assertion methods.
/// This allows using the full assertion builder (e.g., item => item.IsGreaterThan(10)) instead of a simple predicate.
/// </summary>
public class CollectionCountWithAssertionValueAssertion<TCollection, TItem> : ValueAssertion<int>
    where TCollection : IEnumerable<TItem>
{
    public CollectionCountWithAssertionValueAssertion(
        AssertionContext<TCollection> collectionContext,
        Func<IAssertionSource<TItem>, Assertion<TItem>?> assertion)
        : base(CreateIntContext(collectionContext, assertion))
    {
    }

    private static AssertionContext<int> CreateIntContext(
        AssertionContext<TCollection> collectionContext,
        Func<IAssertionSource<TItem>, Assertion<TItem>?> assertion)
    {
        return collectionContext.Map<int>(async collection =>
        {
            if (collection == null)
            {
                return 0;
            }

            int count = 0;
            int index = 0;

            foreach (var item in collection)
            {
                var itemAssertion = new ValueAssertion<TItem>(item, $"item[{index}]");
                var resultingAssertion = assertion(itemAssertion);

                if (resultingAssertion != null)
                {
                    try
                    {
                        await resultingAssertion.AssertAsync();
                        count++;
                    }
                    catch
                    {
                        // Item did not satisfy the assertion, don't count it
                    }
                }
                else
                {
                    // Null assertion means no constraint, count all items
                    count++;
                }

                index++;
            }

            return count;
        });
    }
}
