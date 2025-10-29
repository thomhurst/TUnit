using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection contains only distinct (unique) items.
/// Inherits from CollectionAssertionBase to enable chaining of collection methods.
/// </summary>
public class HasDistinctItemsAssertion<TCollection, TItem> : Sources.CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    public HasDistinctItemsAssertion(
        AssertionContext<TCollection> context)
        : base(context)
    {
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

        var seen = new HashSet<TItem>();
        var duplicates = new List<TItem>();
        var totalCount = 0;

        foreach (var item in value)
        {
            totalCount++;
            if (!seen.Add(item) && !duplicates.Contains(item))
            {
                duplicates.Add(item);
            }
        }

        if (duplicates.Count == 0)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"found {totalCount - seen.Count} duplicate(s): {string.Join(", ", duplicates)}"));
    }

    protected override string GetExpectation() => "to have distinct items";
}
