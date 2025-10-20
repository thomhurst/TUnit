using System.Diagnostics.CodeAnalysis;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Conditions.Helpers;
using TUnit.Assertions.Core;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection is NOT equivalent to another collection.
/// Two collections are considered NOT equivalent if they differ in elements or (optionally) their order.
/// Inherits from CollectionComparerBasedAssertion to preserve collection type awareness in And/Or chains.
/// </summary>
[AssertionExtension("IsNotEquivalentTo")]
[RequiresDynamicCode("Collection equivalency uses structural comparison for complex objects, which requires reflection and is not compatible with AOT")]
public class NotEquivalentToAssertion<TItem> : CollectionComparerBasedAssertion<TItem>
{
    private readonly IEnumerable<TItem> _notExpected;
    private readonly CollectionOrdering _ordering;

    public NotEquivalentToAssertion(
        AssertionContext<IEnumerable<TItem>> context,
        IEnumerable<TItem> notExpected,
        CollectionOrdering ordering = CollectionOrdering.Any)
        : base(context)
    {
        _notExpected = notExpected ?? throw new ArgumentNullException(nameof(notExpected));
        _ordering = ordering;
    }

    public NotEquivalentToAssertion<TItem> Using(IEqualityComparer<TItem> comparer)
    {
        SetComparer(comparer);
        return this;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Collection equivalency uses structural comparison which requires reflection")]
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<IEnumerable<TItem>> metadata)
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
            _notExpected,
            _ordering,
            comparer);

        // Invert the logic: we want them to NOT be equivalent
        return Task.FromResult(result.AreEquivalent
            ? AssertionResult.Failed("collections are equivalent but should not be")
            : AssertionResult.Passed);
    }

    protected override string GetExpectation() =>
        $"to not be equivalent to [{string.Join(", ", _notExpected)}]";
}
