using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// Helper class for checking collection equivalency.
/// Provides reusable logic for comparing collections with different ordering requirements.
/// </summary>
internal static class CollectionEquivalencyChecker
{
    /// <summary>
    /// Result of a collection equivalency check.
    /// </summary>
    public record CheckResult
    {
        public bool AreEquivalent { get; init; }
        public string? ErrorMessage { get; init; }

        public static CheckResult Success() => new() { AreEquivalent = true };
        public static CheckResult Failure(string message) => new() { AreEquivalent = false, ErrorMessage = message };
    }

    /// <summary>
    /// Checks if two collections are equivalent based on the specified ordering requirement.
    /// </summary>
    public static CheckResult AreEquivalent<TItem>(
        IEnumerable<TItem>? actual,
        IEnumerable<TItem> expected,
        CollectionOrdering ordering,
        IEqualityComparer<TItem> comparer)
    {
        if (actual == null)
        {
            return CheckResult.Failure("collection was null");
        }

        // Optimize for collections that are already lists to avoid re-enumeration
        var actualList = actual is List<TItem> actualListCasted ? actualListCasted : actual.ToList();
        var expectedList = expected is List<TItem> expectedListCasted ? expectedListCasted : expected.ToList();

        // Check counts first
        if (actualList.Count != expectedList.Count)
        {
            return CheckResult.Failure(
                $"collection has {actualList.Count} items but expected {expectedList.Count}");
        }

        // If ordering must match, check items in order
        if (ordering == CollectionOrdering.Matching)
        {
            return CheckOrderedEquivalence(actualList, expectedList, comparer);
        }

        // Otherwise, use frequency-based comparison (CollectionOrdering.Any)
        return CheckUnorderedEquivalence(actualList, expectedList, comparer);
    }

    private static CheckResult CheckOrderedEquivalence<TItem>(
        List<TItem> actualList,
        List<TItem> expectedList,
        IEqualityComparer<TItem> comparer)
    {
        for (int i = 0; i < actualList.Count; i++)
        {
            var actualItem = actualList[i];
            var expectedItem = expectedList[i];

            bool areEqual = actualItem == null && expectedItem == null ||
                           actualItem != null && expectedItem != null && comparer.Equals(actualItem, expectedItem);

            if (!areEqual)
            {
                return CheckResult.Failure(
                    $"collection item at index {i} does not match: expected {expectedItem}, but was {actualItem}");
            }
        }

        return CheckResult.Success();
    }

    private static CheckResult CheckUnorderedEquivalence<TItem>(
        List<TItem> actualList,
        List<TItem> expectedList,
        IEqualityComparer<TItem> comparer)
    {
        // When using a custom comparer, we use a linear search approach because:
        // 1. Custom comparers (especially tolerance-based ones for floating-point) often cannot
        //    implement GetHashCode correctly (equal items MUST have same hash code)
        // 2. Dictionary lookups rely on both GetHashCode and Equals, which fails with broken hash codes
        // 3. Linear search is more forgiving and aligns with user expectations for custom comparers
        var isDefaultComparer = ReferenceEquals(comparer, EqualityComparer<TItem>.Default);

        if (!isDefaultComparer)
        {
            return CheckUnorderedEquivalenceLinear(actualList, expectedList, comparer);
        }

        // Use efficient Dictionary-based frequency map for default comparer - O(n)
        return CheckUnorderedEquivalenceDictionary(actualList, expectedList, comparer);
    }

    private static CheckResult CheckUnorderedEquivalenceLinear<TItem>(
        List<TItem> actualList,
        List<TItem> expectedList,
        IEqualityComparer<TItem> comparer)
    {
        // Use linear search for custom comparers - O(n²) but correct
        var remainingActual = new List<TItem>(actualList);

        foreach (var expectedItem in expectedList)
        {
            var foundIndex = -1;
            for (int i = 0; i < remainingActual.Count; i++)
            {
                var actualItem = remainingActual[i];

                bool areEqual = expectedItem == null && actualItem == null ||
                               expectedItem != null && actualItem != null && comparer.Equals(expectedItem, actualItem);

                if (areEqual)
                {
                    foundIndex = i;
                    break;
                }
            }

            if (foundIndex == -1)
            {
                return CheckResult.Failure(
                    $"collection does not contain expected item: {expectedItem}");
            }

            remainingActual.RemoveAt(foundIndex);
        }

        return CheckResult.Success();
    }

    private static CheckResult CheckUnorderedEquivalenceDictionary<TItem>(
        List<TItem> actualList,
        List<TItem> expectedList,
        IEqualityComparer<TItem> comparer)
    {
        // Build a frequency map of actual items
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
                    return CheckResult.Failure(
                        "collection does not contain expected null item");
                }
                nullCount--;
            }
            else
            {
                if (!actualCounts.TryGetValue(expectedItem, out var count) || count == 0)
                {
                    return CheckResult.Failure(
                        $"collection does not contain expected item: {expectedItem}");
                }
                actualCounts[expectedItem] = count - 1;
            }
        }

        return CheckResult.Success();
    }
}
