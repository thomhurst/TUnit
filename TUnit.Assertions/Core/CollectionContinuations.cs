using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for collection assertions that preserves collection instance methods.
/// Inherits from CollectionAssertionBase to provide collection-specific methods throughout the chain.
/// </summary>
public class CollectionAndContinuation<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    internal CollectionAndContinuation(AssertionContext<TCollection> context, CollectionAssertionBase<TCollection, TItem> previousAssertion)
        : base(context)
    {
        context.ExpressionBuilder.Append(".And");
        // Set pending link for the next assertion to consume
        context.SetPendingLink(previousAssertion, CombinerType.And);
    }

    protected override string GetExpectation()
    {
        return "collection and continuation";
    }
}

/// <summary>
/// Or continuation for collection assertions that preserves collection instance methods.
/// Inherits from CollectionAssertionBase to provide collection-specific methods throughout the chain.
/// </summary>
public class CollectionOrContinuation<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    internal CollectionOrContinuation(AssertionContext<TCollection> context, CollectionAssertionBase<TCollection, TItem> previousAssertion)
        : base(context)
    {
        context.ExpressionBuilder.Append(".Or");
        // Set pending link for the next assertion to consume
        context.SetPendingLink(previousAssertion, CombinerType.Or);
    }

    protected override string GetExpectation()
    {
        return "collection or continuation";
    }
}
