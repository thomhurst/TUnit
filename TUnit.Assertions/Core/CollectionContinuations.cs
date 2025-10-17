using System.Collections;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for collection assertions that preserves collection type and item type.
/// Implements ICollectionAssertionSource to enable all collection extension methods.
/// </summary>
public class CollectionAndContinuation<TCollection, TItem> : ContinuationBase<TCollection>, ICollectionAssertionSource<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    internal CollectionAndContinuation(AssertionContext<TCollection> context, Assertion<TCollection> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}

/// <summary>
/// Or continuation for collection assertions that preserves collection type and item type.
/// Implements ICollectionAssertionSource to enable all collection extension methods.
/// </summary>
public class CollectionOrContinuation<TCollection, TItem> : ContinuationBase<TCollection>, ICollectionAssertionSource<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    internal CollectionOrContinuation(AssertionContext<TCollection> context, Assertion<TCollection> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
