using System.Collections;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for collection assertions that preserves collection type and item type.
/// Inherits from CollectionAssertionBase to automatically expose all collection methods.
/// </summary>
public class CollectionAndContinuation<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    internal CollectionAndContinuation(AssertionContext<TCollection> context, Assertion<TCollection> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}

/// <summary>
/// Or continuation for collection assertions that preserves collection type and item type.
/// Inherits from CollectionAssertionBase to automatically expose all collection methods.
/// </summary>
public class CollectionOrContinuation<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    internal CollectionOrContinuation(AssertionContext<TCollection> context, Assertion<TCollection> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
