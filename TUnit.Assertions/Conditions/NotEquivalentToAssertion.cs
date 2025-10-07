using System.Text;
using TUnit.Assertions.Core;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection is NOT equivalent to another collection.
/// Two collections are considered NOT equivalent if they differ in elements or (optionally) their order.
/// </summary>
public class NotEquivalentToAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly IEnumerable<TItem> _notExpected;
    private readonly CollectionOrdering _ordering;
    private IEqualityComparer<TItem>? _comparer;

    public NotEquivalentToAssertion(
        EvaluationContext<TCollection> context,
        IEnumerable<TItem> notExpected,
        StringBuilder expressionBuilder,
        CollectionOrdering ordering = CollectionOrdering.Any)
        : base(context, expressionBuilder)
    {
        _notExpected = notExpected ?? throw new ArgumentNullException(nameof(notExpected));
        _ordering = ordering;
    }

    public NotEquivalentToAssertion<TCollection, TItem> Using(IEqualityComparer<TItem> comparer)
    {
        _comparer = comparer;
        ExpressionBuilder.Append($".Using({comparer.GetType().Name})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(TCollection? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("collection was null"));

        var comparer = _comparer ?? EqualityComparer<TItem>.Default;

        var actualList = value.ToList();
        var expectedList = _notExpected.ToList();

        // Check counts first
        if (actualList.Count != expectedList.Count)
        {
            // Different counts means NOT equivalent - this is what we want
            return Task.FromResult(AssertionResult.Passed);
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
                    // Found a difference - collections are NOT equivalent, which is what we want
                    return Task.FromResult(AssertionResult.Passed);
                }
            }

            // All items matched in order - collections ARE equivalent, which is NOT what we want
            return Task.FromResult(AssertionResult.Failed(
                "collections are equivalent but should not be"));
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
                    // Missing null item - collections are NOT equivalent, which is what we want
                    return Task.FromResult(AssertionResult.Passed);
                }
                nullCount--;
            }
            else
            {
                if (!actualCounts.TryGetValue(expectedItem, out var count) || count == 0)
                {
                    // Missing or insufficient item - collections are NOT equivalent, which is what we want
                    return Task.FromResult(AssertionResult.Passed);
                }
                actualCounts[expectedItem] = count - 1;
            }
        }

        // All items matched - collections ARE equivalent, which is NOT what we want
        return Task.FromResult(AssertionResult.Failed(
            "collections are equivalent but should not be"));
    }

    protected override string GetExpectation() =>
        $"to not be equivalent to [{string.Join(", ", _notExpected)}]";
}
