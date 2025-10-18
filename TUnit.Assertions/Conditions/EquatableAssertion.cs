using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value implementing IEquatable&lt;TExpected&gt; is equal to an expected value of a different type.
/// This allows comparing types that implement IEquatable with cross-type equality.
/// Example: A Wrapper struct implementing IEquatable&lt;long&gt; can be compared directly to a long value.
/// </summary>
[AssertionExtension("IsEquatableTo")]
public class EquatableAssertion<TActual, TExpected> : Assertion<TActual>
    where TActual : IEquatable<TExpected>
{
    private readonly TExpected _expected;

    public EquatableAssertion(
        AssertionContext<TActual> context,
        TExpected expected)
        : base(context)
    {
        _expected = expected;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TActual> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().FullName}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        // Use IEquatable<TExpected>.Equals for comparison
        if (value.Equals(_expected))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => $"to be equal to {_expected}";
}

/// <summary>
/// Asserts that a nullable value type implementing IEquatable&lt;TExpected&gt; is equal to an expected value.
/// Handles nullable structs that implement IEquatable.
/// </summary>
[AssertionExtension("IsEquatableTo")]
public class NullableEquatableAssertion<TActual, TExpected> : Assertion<TActual?>
    where TActual : struct, IEquatable<TExpected>
{
    private readonly TExpected _expected;

    public NullableEquatableAssertion(
        AssertionContext<TActual?> context,
        TExpected expected)
        : base(context)
    {
        _expected = expected;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TActual?> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().FullName}"));
        }

        if (!value.HasValue)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        // Use IEquatable<TExpected>.Equals for comparison
        if (value.Value.Equals(_expected))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {value.Value}"));
    }

    protected override string GetExpectation() => $"to be equal to {_expected}";
}
