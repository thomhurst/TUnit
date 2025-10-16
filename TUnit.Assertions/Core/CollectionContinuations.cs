using System.Collections;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for collection assertions that preserves collection type and item type.
/// Implements ICollectionAssertionSource to enable all collection extension methods.
/// </summary>
public class CollectionAndContinuation<TCollection, TItem> : ICollectionAssertionSource<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public AssertionContext<TCollection> Context { get; }

    internal CollectionAndContinuation(AssertionContext<TCollection> context, Assertion<TCollection> previousAssertion)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        context.ExpressionBuilder.Append(".And");
        // Set pending link for the next assertion to consume
        context.SetPendingLink(previousAssertion, CombinerType.And);
    }
}

/// <summary>
/// Or continuation for collection assertions that preserves collection type and item type.
/// Implements ICollectionAssertionSource to enable all collection extension methods.
/// </summary>
public class CollectionOrContinuation<TCollection, TItem> : ICollectionAssertionSource<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public AssertionContext<TCollection> Context { get; }

    internal CollectionOrContinuation(AssertionContext<TCollection> context, Assertion<TCollection> previousAssertion)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        context.ExpressionBuilder.Append(".Or");
        // Set pending link for the next assertion to consume
        context.SetPendingLink(previousAssertion, CombinerType.Or);
    }
}
