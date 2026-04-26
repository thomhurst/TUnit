using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// Helper that finds the first differing member path between two object graphs.
/// Used to surface focused diff messages instead of dumping entire serialized objects
/// in failure messages for IsEqualTo / IsEquivalentTo assertions.
/// </summary>
internal static class StructuralDiffHelper
{
    /// <summary>
    /// Result of a structural diff search.
    /// </summary>
    public readonly struct StructuralDiffResult
    {
        public bool HasDiff { get; }
        public string Path { get; }
        public object? ExpectedValue { get; }
        public object? ActualValue { get; }
        public string Reason { get; }

        private StructuralDiffResult(bool hasDiff, string path, object? expectedValue, object? actualValue, string reason)
        {
            HasDiff = hasDiff;
            Path = path;
            ExpectedValue = expectedValue;
            ActualValue = actualValue;
            Reason = reason;
        }

        public static StructuralDiffResult None { get; } = new(false, string.Empty, null, null, string.Empty);

        public static StructuralDiffResult Found(string path, object? expectedValue, object? actualValue, string reason = "")
            => new(true, path, expectedValue, actualValue, reason);
    }

    /// <summary>
    /// Finds the first differing member between <paramref name="actual"/> and <paramref name="expected"/>.
    /// Returns <see cref="StructuralDiffResult.None"/> when no diff is found (e.g., types are primitives that
    /// already report directly, or the comparison cannot be safely structural).
    /// </summary>
    /// <remarks>
    /// This is a best-effort, allocation-light traversal: we only recurse into reference types whose
    /// public members can be reflected. For value types and well-known immutable types, we report the
    /// raw value pair without further recursion. Cycles are guarded via reference tracking.
    /// </remarks>
    [RequiresUnreferencedCode("Structural diff uses reflection to inspect object members and is not compatible with AOT")]
    public static StructuralDiffResult FindFirstDifference(object? actual, object? expected)
    {
        var visitedActual = new HashSet<object>(ReferenceEqualityComparer<object>.Instance);
        var visitedExpected = new HashSet<object>(ReferenceEqualityComparer<object>.Instance);
        return FindFirstDifference(actual, expected, string.Empty, visitedActual, visitedExpected);
    }

    [RequiresUnreferencedCode("Structural diff uses reflection to inspect object members and is not compatible with AOT")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Structural diff intentionally inspects runtime types reflectively")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Structural diff intentionally inspects runtime types reflectively")]
    private static StructuralDiffResult FindFirstDifference(object? actual, object? expected, string path, HashSet<object> visitedActual, HashSet<object> visitedExpected)
    {
        if (ReferenceEquals(actual, expected))
        {
            return StructuralDiffResult.None;
        }

        if (actual is null || expected is null)
        {
            return StructuralDiffResult.Found(path, expected, actual, "one value is null");
        }

        var actualType = actual.GetType();
        var expectedType = expected.GetType();

        if (actualType != expectedType)
        {
            return StructuralDiffResult.Found(path, expected, actual, $"types differ ({expectedType.Name} vs {actualType.Name})");
        }

        if (TypeHelper.IsPrimitiveOrWellKnownType(actualType))
        {
            return Equals(actual, expected)
                ? StructuralDiffResult.None
                : StructuralDiffResult.Found(path, expected, actual);
        }

        // Cycle guard — track both sides so a self-referential expected graph cannot loop
        // forever even if the actual side is acyclic (mirrors StructuralEquivalencyAssertion).
        // We cannot prove inequality from inside a cycle, so report no diff at this branch.
        if (!visitedActual.Add(actual) || !visitedExpected.Add(expected))
        {
            return StructuralDiffResult.None;
        }

        if (actual is IEnumerable actualEnumerable && expected is IEnumerable expectedEnumerable
            && actual is not string && expected is not string)
        {
            return FindEnumerableDifference(actualEnumerable, expectedEnumerable, path, visitedActual, visitedExpected);
        }

        var members = ReflectionHelper.GetMembersToCompare(actualType);
        if (members.Length == 0)
        {
            // No reflectable surface — fall back to Equals(). If they are not equal, surface the
            // raw values; we have nothing more granular to offer.
            return Equals(actual, expected)
                ? StructuralDiffResult.None
                : StructuralDiffResult.Found(path, expected, actual);
        }

        foreach (var member in members)
        {
            var memberPath = string.IsNullOrEmpty(path) ? member.Name : $"{path}.{member.Name}";
            var actualValue = ReflectionHelper.GetMemberValue(actual, member);
            var expectedValue = ReflectionHelper.GetMemberValue(expected, member);

            var nested = FindFirstDifference(actualValue, expectedValue, memberPath, visitedActual, visitedExpected);
            if (nested.HasDiff)
            {
                return nested;
            }
        }

        return StructuralDiffResult.None;
    }

    [RequiresUnreferencedCode("Structural diff uses reflection to inspect object members and is not compatible with AOT")]
    private static StructuralDiffResult FindEnumerableDifference(IEnumerable actual, IEnumerable expected, string path, HashSet<object> visitedActual, HashSet<object> visitedExpected)
    {
        var actualEnumerator = actual.GetEnumerator();
        var expectedEnumerator = expected.GetEnumerator();

        try
        {
            var index = 0;
            while (true)
            {
                var actualHasNext = actualEnumerator.MoveNext();
                var expectedHasNext = expectedEnumerator.MoveNext();

                if (!actualHasNext && !expectedHasNext)
                {
                    return StructuralDiffResult.None;
                }

                var indexPath = $"{path}[{index}]";

                if (!actualHasNext)
                {
                    return StructuralDiffResult.Found(indexPath, expectedEnumerator.Current, null, "actual ended early");
                }

                if (!expectedHasNext)
                {
                    return StructuralDiffResult.Found(indexPath, null, actualEnumerator.Current, "actual has extra item");
                }

                var nested = FindFirstDifference(actualEnumerator.Current, expectedEnumerator.Current, indexPath, visitedActual, visitedExpected);
                if (nested.HasDiff)
                {
                    return nested;
                }

                index++;
            }
        }
        finally
        {
            (actualEnumerator as IDisposable)?.Dispose();
            (expectedEnumerator as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Formats a diff result as a "Expected ... but found ..." style message fragment.
    /// Returns null when there is no diff to format.
    /// </summary>
    public static string? FormatDiff(StructuralDiffResult diff)
    {
        if (!diff.HasDiff)
        {
            return null;
        }

        var location = string.IsNullOrEmpty(diff.Path) ? "value" : $"member {diff.Path}";
        var message = $"differs at {location}: expected {FormatValue(diff.ExpectedValue)} but found {FormatValue(diff.ActualValue)}";
        if (!string.IsNullOrEmpty(diff.Reason))
        {
            message += $" — {diff.Reason}";
        }
        return message;
    }

    internal static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => value.ToString() ?? "null",
    };

    /// <summary>
    /// Single-pass walk over top-level members of <paramref name="actual"/> and
    /// <paramref name="expected"/>, returning both a similarity score (count of top-level
    /// members that compare equal under <see cref="object.Equals(object?, object?)"/>) and
    /// the first nested structural diff. Used by the "closest match" hint to avoid two
    /// reflection walks per candidate.
    /// </summary>
    [RequiresUnreferencedCode("Structural diff uses reflection to inspect object members and is not compatible with AOT")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Structural diff intentionally inspects runtime types reflectively")]
    public static (int Score, StructuralDiffResult Diff) ScoreAndDiff(object? actual, object? expected)
    {
        if (actual is null || expected is null)
        {
            return (0, FindFirstDifference(actual, expected));
        }

        var type = actual.GetType();
        if (type != expected.GetType())
        {
            return (0, FindFirstDifference(actual, expected));
        }

        var members = ReflectionHelper.GetMembersToCompare(type);
        if (members.Length == 0)
        {
            return (0, FindFirstDifference(actual, expected));
        }

        var visitedActual = new HashSet<object>(ReferenceEqualityComparer<object>.Instance) { actual };
        var visitedExpected = new HashSet<object>(ReferenceEqualityComparer<object>.Instance) { expected };

        var matches = 0;
        var firstDiff = StructuralDiffResult.None;
        foreach (var member in members)
        {
            var actualValue = ReflectionHelper.GetMemberValue(actual, member);
            var expectedValue = ReflectionHelper.GetMemberValue(expected, member);

            // Match the scoring semantics of CountMatchingTopLevelMembers: a top-level member
            // counts as "matching" iff Equals reports it equal. This avoids paying for a deep
            // recursion on members that already compare equal at the top level.
            if (Equals(actualValue, expectedValue))
            {
                matches++;
                continue;
            }

            // Only descend for the first differing member — once we have a diff, additional
            // recursion is wasted work since we only return the first one.
            if (!firstDiff.HasDiff)
            {
                firstDiff = FindFirstDifference(actualValue, expectedValue, member.Name, visitedActual, visitedExpected);
            }
        }

        return (matches, firstDiff);
    }
}
