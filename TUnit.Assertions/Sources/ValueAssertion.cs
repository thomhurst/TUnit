using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for immediate values.
/// This is the entry point for: Assert.That(value)
/// </summary>
public class ValueAssertion<TValue> : Assertion<TValue>
{
    public ValueAssertion(TValue? value, string? expression)
        : base(new EvaluationContext<TValue>(value))
    {
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        // Source assertions don't perform checks - they just provide the value
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have a value";
}
