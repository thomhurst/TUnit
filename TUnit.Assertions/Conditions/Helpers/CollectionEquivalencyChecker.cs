using System.Diagnostics.CodeAnalysis;
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
                var diff = DescribeItemDifference(expectedItem, actualItem);
                return CheckResult.Failure(
                    $"collection item at index {i} does not match: expected {expectedItem}, but was {actualItem}{diff}");
            }
        }

        return CheckResult.Success();
    }

    /// <summary>
    /// Adds a focused structural diff to the failure message when both items are non-null
    /// reference objects with reflectable members. Returns an empty string for primitives or
    /// well-known types — those already render as "expected X but was Y" via the caller's
    /// message, so a structural diff would be redundant.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Structural diff is best-effort; gracefully degrades when reflection is unavailable")]
    private static string DescribeItemDifference<TItem>(TItem expected, TItem actual)
    {
        if (expected is null || actual is null)
        {
            return string.Empty;
        }

        var type = expected.GetType();
        if (TypeHelper.IsPrimitiveOrWellKnownType(type))
        {
            return string.Empty;
        }

        var diff = StructuralDiffHelper.FindFirstDifference(actual, expected);
        var formatted = StructuralDiffHelper.FormatDiff(diff);
        return formatted is null ? string.Empty : $" ({formatted})";
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
                // Score the closest match against items still unmatched — items already
                // paired up earlier in the loop are by definition not the candidate we want.
                var diff = DescribeClosestDiff(expectedItem, remainingActual);
                return CheckResult.Failure(
                    $"collection does not contain expected item: {expectedItem}{diff}");
            }

            remainingActual.RemoveAt(foundIndex);
        }

        return CheckResult.Success();
    }

    /// <summary>
    /// Finds the candidate in <paramref name="candidates"/> with the highest "similarity score"
    /// to <paramref name="expected"/> and returns a parenthesized hint pointing to the diff for
    /// that candidate. Similarity counts top-level members that match exactly — this picks the
    /// candidate that "almost matches" rather than an unrelated item. Returns empty when no
    /// useful hint can be produced (primitives, no reflectable members, or no candidate of the
    /// matching type).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Structural diff is best-effort; gracefully degrades when reflection is unavailable")]
    private static string DescribeClosestDiff<TItem>(TItem expected, IReadOnlyList<TItem> candidates)
    {
        if (expected is null || candidates.Count == 0)
        {
            return string.Empty;
        }

        var expectedType = expected.GetType();
        if (TypeHelper.IsPrimitiveOrWellKnownType(expectedType))
        {
            return string.Empty;
        }

        StructuralDiffHelper.StructuralDiffResult bestDiff = default;
        var bestScore = -1;
        TItem? bestCandidate = default;

        foreach (var candidate in candidates)
        {
            if (candidate is null || candidate.GetType() != expectedType)
            {
                continue;
            }

            var diff = StructuralDiffHelper.FindFirstDifference(candidate, expected);
            if (!diff.HasDiff)
            {
                continue;
            }

            var score = StructuralDiffHelper.CountMatchingTopLevelMembers(candidate, expected);
            if (score > bestScore)
            {
                bestScore = score;
                bestDiff = diff;
                bestCandidate = candidate;
            }
        }

        // Require at least one matching top-level member before we claim a "closest match".
        // CountMatchingTopLevelMembers returns 0 for completely dissimilar candidates, and a
        // bestScore of 0 beating the initial -1 would otherwise produce misleading hints
        // pointing at unrelated objects.
        if (bestScore < 1)
        {
            return string.Empty;
        }

        var formatted = StructuralDiffHelper.FormatDiff(bestDiff);
        if (formatted is null)
        {
            return string.Empty;
        }

        return $" (closest match {bestCandidate} {formatted})";
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
                    // Materialize the still-unconsumed items from the frequency map so
                    // DescribeClosestDiff cannot point at items already paired up by an
                    // earlier expected entry. Built only on the failure path so the
                    // success path stays O(n).
                    var remaining = ExpandRemaining(actualCounts, nullCount);
                    var diff = DescribeClosestDiff(expectedItem, remaining);
                    return CheckResult.Failure(
                        $"collection does not contain expected item: {expectedItem}{diff}");
                }
                actualCounts[expectedItem] = count - 1;
            }
        }

        return CheckResult.Success();
    }

#pragma warning disable CS8714 // Nullability of type argument doesn't match 'notnull' constraint - we handle nulls separately
    private static List<TItem> ExpandRemaining<TItem>(Dictionary<TItem, int> counts, int nullCount)
#pragma warning restore CS8714
    {
        var remaining = new List<TItem>(counts.Count + nullCount);
        for (int i = 0; i < nullCount; i++)
        {
            remaining.Add(default!);
        }
        foreach (var pair in counts)
        {
            for (int i = 0; i < pair.Value; i++)
            {
                remaining.Add(pair.Key);
            }
        }
        return remaining;
    }
}
