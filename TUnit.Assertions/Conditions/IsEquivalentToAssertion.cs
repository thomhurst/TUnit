using System.Text;
using TUnit.Assertions.Core;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection is equivalent to another collection.
/// Two collections are equivalent if they contain the same elements, regardless of order (default).
/// Can be configured to require matching order using CollectionOrdering.Matching.
/// </summary>
public class IsEquivalentToAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly IEnumerable<TItem> _expected;
    private readonly CollectionOrdering _ordering;
    private IEqualityComparer<TItem>? _comparer;

    public IsEquivalentToAssertion(
        AssertionContext<TCollection> context,
        IEnumerable<TItem> expected,
        CollectionOrdering ordering = CollectionOrdering.Any)
        : base(context)
    {
        _expected = expected ?? throw new ArgumentNullException(nameof(expected));
        _ordering = ordering;
    }

    public IsEquivalentToAssertion<TCollection, TItem> Using(IEqualityComparer<TItem> comparer)
    {
        _comparer = comparer;
        Context.ExpressionBuilder.Append($".Using({comparer.GetType().Name})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("collection was null"));
        }

        var comparer = _comparer ?? EqualityComparer<TItem>.Default;

        var actualList = value.ToList();
        var expectedList = _expected.ToList();

        // Check counts first
        if (actualList.Count != expectedList.Count)
        {
            return Task.FromResult(AssertionResult.Failed(
                $"collection has {actualList.Count} items but expected {expectedList.Count}"));
        }

        // If ordering must match, check items in order
        if (_ordering == CollectionOrdering.Matching)
        {
            for (int i = 0; i < actualList.Count; i++)
            {
                var actualItem = actualList[i];
                var expectedItem = expectedList[i];

                bool areEqual = actualItem == null && expectedItem == null ||
                               actualItem != null && expectedItem != null && comparer.Equals(actualItem, expectedItem);

                if (!areEqual)
                {
                    return Task.FromResult(AssertionResult.Failed(
                        $"collection item at index {i} does not match: expected {expectedItem}, but was {actualItem}"));
                }
            }

            return Task.FromResult(AssertionResult.Passed);
        }

        // Otherwise, use frequency map for unordered comparison (CollectionOrdering.Any)
        // Build a frequency map of actual items - O(n)
        // Track null count separately to avoid Dictionary<TKey> notnull constraint
        int nullCount = 0;
#pragma warning disable CS8714 // Nullability of type argument doesn't match 'notnull' constraint - we handle nulls separately
        var actualCounts = new Dictionary<TItem, int>(comparer);
#pragma warning restore CS8714

        foreach (var item in actualList)
        {
            if (item == null)
            {
                nullCount++;
            }
            else
            {
                if (actualCounts.TryGetValue(item, out var count))
                {
                    actualCounts[item] = count + 1;
                }
                else
                {
                    actualCounts[item] = 1;
                }
            }
        }

        // Check if all expected items are present with correct frequency - O(n)
        foreach (var expectedItem in expectedList)
        {
            if (expectedItem == null)
            {
                if (nullCount == 0)
                {
                    return Task.FromResult(AssertionResult.Failed(
                        "collection does not contain expected null item"));
                }
                nullCount--;
            }
            else
            {
                if (!actualCounts.TryGetValue(expectedItem, out var count) || count == 0)
                {
                    return Task.FromResult(AssertionResult.Failed(
                        $"collection does not contain expected item: {expectedItem}"));
                }
                actualCounts[expectedItem] = count - 1;
            }
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() =>
        $"to be equivalent to [{string.Join(", ", _expected)}]";
}
