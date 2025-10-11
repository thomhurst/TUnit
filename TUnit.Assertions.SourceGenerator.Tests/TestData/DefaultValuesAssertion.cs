using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Assertion with various default parameter values
/// Should generate extension method with properly formatted defaults
/// </summary>
[AssertionExtension("HasDefaultValues")]
public class DefaultValuesAssertion<TValue> : Assertion<TValue>
{
    private readonly bool _boolValue;
    private readonly int _intValue;
    private readonly string _stringValue;

    public DefaultValuesAssertion(
        AssertionContext<TValue> context,
        bool boolValue = true,
        int intValue = 0,
        string stringValue = "default")
        : base(context)
    {
        _boolValue = boolValue;
        _intValue = intValue;
        _stringValue = stringValue;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have default values";
}
