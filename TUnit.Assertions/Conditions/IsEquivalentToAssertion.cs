using System.Diagnostics.CodeAnalysis;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Conditions.Helpers;
using TUnit.Assertions.Core;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection is equivalent to another collection.
/// Two collections are equivalent if they contain the same elements, regardless of order (default).
/// Can be configured to require matching order using CollectionOrdering.Matching.
/// Inherits from CollectionComparerBasedAssertion to preserve collection type awareness in And/Or chains.
/// </summary>
[AssertionExtension("IsEquivalentTo")]
[RequiresDynamicCode("Collection equivalency uses structural comparison for complex objects, which requires reflection and is not compatible with AOT")]
public class IsEquivalentToAssertion<TCollection, TItem> : CollectionComparerBasedAssertion<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly IEnumerable<TItem> _expected;
    private readonly CollectionOrdering _ordering;

    public IsEquivalentToAssertion(
        AssertionContext<TCollection> context,
        IEnumerable<TItem> expected,
        CollectionOrdering ordering = CollectionOrdering.Any)
        : base(context)
    {
        _expected = expected ?? throw new ArgumentNullException(nameof(expected));
        _ordering = ordering;
    }

    public IsEquivalentToAssertion(
        AssertionContext<TCollection> context,
        IEnumerable<TItem> expected,
        IEqualityComparer<TItem> comparer,
        CollectionOrdering ordering = CollectionOrdering.Any)
        : base(context)
    {
        _expected = expected ?? throw new ArgumentNullException(nameof(expected));
        _ordering = ordering;
        SetComparer(comparer);
    }

    public IsEquivalentToAssertion<TCollection, TItem> Using(IEqualityComparer<TItem> comparer)
    {
        SetComparer(comparer);
        return this;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Collection equivalency uses structural comparison which requires reflection")]
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var comparer = HasCustomComparer() ? GetComparer() : StructuralEqualityComparer<TItem>.Instance;

        var result = CollectionEquivalencyChecker.AreEquivalent(
            value,
            _expected,
            _ordering,
            comparer);

        return Task.FromResult(result.AreEquivalent
            ? AssertionResult.Passed
            : AssertionResult.Failed(result.ErrorMessage!));
    }

    protected override string GetExpectation() =>
        $"to be equivalent to [{string.Join(", ", _expected)}]";
}
