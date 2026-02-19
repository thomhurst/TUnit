using TUnit.Assertions.Adapters;
using TUnit.Assertions.Collections;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection contains only distinct (unique) items.
/// Delegates to CollectionChecks for the actual logic.
/// </summary>
public class HasDistinctItemsAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly IEqualityComparer<TItem>? _comparer;

    public HasDistinctItemsAssertion(
        AssertionContext<TCollection> context)
        : base(context)
    {
    }

    public HasDistinctItemsAssertion(
        AssertionContext<TCollection> context,
        IEqualityComparer<TItem> comparer)
        : base(context)
    {
        _comparer = comparer;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().Name}"));
        }

        if (metadata.Value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var adapter = new EnumerableAdapter<TItem>(metadata.Value);
        return Task.FromResult(CollectionChecks.CheckHasDistinctItems(adapter, _comparer));
    }

    protected override string GetExpectation() => "to have distinct items";
}
