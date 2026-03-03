using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Assertions.Enums;

/// <summary>
/// Asserts that an enum value is defined in its enum type.
/// </summary>
public class IsDefinedAssertion<TEnum> : Assertion<TEnum>
    where TEnum : struct, Enum
{
    public IsDefinedAssertion(
        AssertionContext<TEnum> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TEnum> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

#if NET
        if (Enum.IsDefined(value))
#else
        if (Enum.IsDefined(typeof(TEnum), value))
#endif
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} is not defined in {typeof(TEnum).Name}"));
    }

    protected override string GetExpectation() => $"to be defined in {typeof(TEnum).Name}";
}

/// <summary>
/// Asserts that an enum value is NOT defined in its enum type.
/// </summary>
public class IsNotDefinedAssertion<TEnum> : Assertion<TEnum>
    where TEnum : struct, Enum
{
    public IsNotDefinedAssertion(
        AssertionContext<TEnum> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TEnum> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

#if NET
        if (!Enum.IsDefined(value))
#else
        if (!Enum.IsDefined(typeof(TEnum), value))
#endif
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} is defined in {typeof(TEnum).Name}"));
    }

    protected override string GetExpectation() => $"to not be defined in {typeof(TEnum).Name}";
}
