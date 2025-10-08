using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for dictionary values.
/// This is the entry point for: Assert.That(dictionary)
/// Knows the TKey and TValue types, enabling better type inference for dictionary operations.
/// </summary>
public class DictionaryAssertion<TKey, TValue> : Assertion<IReadOnlyDictionary<TKey, TValue>>
{
    public DictionaryAssertion(IReadOnlyDictionary<TKey, TValue> value, string? expression)
        : base(new EvaluationContext<IReadOnlyDictionary<TKey, TValue>>(value))
    {
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<IReadOnlyDictionary<TKey, TValue>> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        // Source assertions don't perform checks - they just provide the value
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have a dictionary value";
}
