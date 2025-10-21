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

        var list = value.ToList();
        var distinctList = list.Distinct().ToList();

        if (list.Count == distinctList.Count)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        var duplicates = list.GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        return Task.FromResult(AssertionResult.Failed(
            $"found {list.Count - distinctList.Count} duplicate(s): {string.Join(", ", duplicates)}"));
    }

    protected override string GetExpectation() => "to have distinct items";
}
