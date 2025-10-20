using System.Collections;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for collection assertions that preserves item type.
/// Inherits from CollectionAssertionBase to automatically expose all collection methods.
/// </summary>
public class CollectionAndContinuation<TItem> : CollectionAssertionBase<TItem>
{
    internal CollectionAndContinuation(AssertionContext<IEnumerable<TItem>> context, Assertion<IEnumerable<TItem>> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}

/// <summary>
/// Or continuation for collection assertions that preserves item type.
/// Inherits from CollectionAssertionBase to automatically expose all collection methods.
/// </summary>
public class CollectionOrContinuation<TItem> : CollectionAssertionBase<TItem>
{
    internal CollectionOrContinuation(AssertionContext<IEnumerable<TItem>> context, Assertion<IEnumerable<TItem>> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
