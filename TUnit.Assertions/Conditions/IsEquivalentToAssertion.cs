using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a collection is equivalent to another collection.
/// Two collections are equivalent if they contain the same elements, regardless of order.
/// </summary>
public class IsEquivalentToAssertion<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly IEnumerable<TItem> _expected;
    private IEqualityComparer<TItem>? _comparer;

    public IsEquivalentToAssertion(
        EvaluationContext<TCollection> context,
        IEnumerable<TItem> expected,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expected = expected ?? throw new ArgumentNullException(nameof(expected));
    }

    public IsEquivalentToAssertion<TCollection, TItem> Using(IEqualityComparer<TItem> comparer)
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
        var expectedList = _expected.ToList();

        // Check counts first
        if (actualList.Count != expectedList.Count)
        {
            return Task.FromResult(AssertionResult.Failed(
                $"collection has {actualList.Count} items but expected {expectedList.Count}"));
        }

        // Check if all expected items are present in actual
        var actualCopy = new List<TItem>(actualList);
        foreach (var expectedItem in expectedList)
        {
            var index = actualCopy.FindIndex(x => comparer.Equals(x, expectedItem));
            if (index < 0)
            {
                return Task.FromResult(AssertionResult.Failed(
                    $"collection does not contain expected item: {expectedItem}"));
            }
            actualCopy.RemoveAt(index);
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() =>
        $"to be equivalent to [{string.Join(", ", _expected)}]";
}
