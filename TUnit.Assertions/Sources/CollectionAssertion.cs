using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for collection values.
/// This is the entry point for: Assert.That(collection)
/// Knows the TItem type, enabling better type inference for collection operations like IsInOrder, All, ContainsOnly.
/// </summary>
public class CollectionAssertion<TItem> : Assertion<IEnumerable<TItem>>
{
    public CollectionAssertion(IEnumerable<TItem> value, string? expression)
        : base(new EvaluationContext<IEnumerable<TItem>>(value))
    {
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }

    protected override Task<AssertionResult> CheckAsync(IEnumerable<TItem>? value, Exception? exception)
    {
        // Source assertions don't perform checks - they just provide the value
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have a collection value";
}
