using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Multiple generic parameters assertion
/// Should generate extension method with two generic type parameters
/// </summary>
[AssertionExtension("IsAssignableTo")]
public class IsAssignableToAssertion<TValue, TTarget> : Assertion<TValue>
{
    private readonly Type _targetType;

    public IsAssignableToAssertion(AssertionContext<TValue> context)
        : base(context)
    {
        _targetType = typeof(TTarget);
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var actualType = value.GetType();

        if (_targetType.IsAssignableFrom(actualType))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"type {actualType.Name} is not assignable to {_targetType.Name}"));
    }

    protected override string GetExpectation() => $"to be assignable to {_targetType.Name}";
}
