using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Single generic parameter assertion
/// Should generate extension method with one generic type parameter
/// </summary>
[AssertionExtension("IsNull")]
public class NullAssertion<TValue> : Assertion<TValue>
{
    public NullAssertion(AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"Value was not null: {value}"));
    }

    protected override string GetExpectation() => "to be null";
}
