using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Assertions.Enums;

/// <summary>
/// Asserts that a flags enum has the specified flag set.
/// </summary>
public class HasFlagAssertion<TEnum> : Assertion<TEnum>
    where TEnum : struct, Enum
{
    private readonly TEnum _expectedFlag;

    public HasFlagAssertion(
        AssertionContext<TEnum> context,
        TEnum expectedFlag)
        : base(context)
    {
        _expectedFlag = expectedFlag;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TEnum> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        // Use HasFlag method for enum flag checking
        var enumValue = (Enum)(object)value;
        var enumFlag = (Enum)(object)_expectedFlag;

        if (enumValue.HasFlag(enumFlag))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} does not have flag {_expectedFlag}"));
    }

    protected override string GetExpectation() => $"to have flag {_expectedFlag}";
}

/// <summary>
/// Asserts that a flags enum does NOT have the specified flag set.
/// </summary>
public class DoesNotHaveFlagAssertion<TEnum> : Assertion<TEnum>
    where TEnum : struct, Enum
{
    private readonly TEnum _unexpectedFlag;

    public DoesNotHaveFlagAssertion(
        AssertionContext<TEnum> context,
        TEnum unexpectedFlag)
        : base(context)
    {
        _unexpectedFlag = unexpectedFlag;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TEnum> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        var enumValue = (Enum)(object)value;
        var enumFlag = (Enum)(object)_unexpectedFlag;

        if (!enumValue.HasFlag(enumFlag))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} has flag {_unexpectedFlag}"));
    }

    protected override string GetExpectation() => $"to not have flag {_unexpectedFlag}";
}

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

        if (Enum.IsDefined(typeof(TEnum), value))
        {
            return Task.FromResult(AssertionResult.Passed);
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

        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value {value} is defined in {typeof(TEnum).Name}"));
    }

    protected override string GetExpectation() => $"to not be defined in {typeof(TEnum).Name}";
}

/// <summary>
/// Asserts that two enum values have the same name.
/// </summary>
public class HasSameNameAsAssertion<TEnum> : Assertion<TEnum>
    where TEnum : struct, Enum
{
    private readonly Enum _otherEnumValue;

    public HasSameNameAsAssertion(
        AssertionContext<TEnum> context,
        Enum otherEnumValue)
        : base(context)
    {
        _otherEnumValue = otherEnumValue;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TEnum> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        var valueName = value.ToString();
        var otherName = _otherEnumValue.ToString();

        if (valueName == otherName)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value name \"{valueName}\" does not equal \"{otherName}\""));
    }

    protected override string GetExpectation() => $"to have the same name as {_otherEnumValue}";
}

/// <summary>
/// Asserts that two enum values have the same underlying value.
/// </summary>
public class HasSameValueAsAssertion<TEnum> : Assertion<TEnum>
    where TEnum : struct, Enum
{
    private readonly Enum _otherEnumValue;

    public HasSameValueAsAssertion(
        AssertionContext<TEnum> context,
        Enum otherEnumValue)
        : base(context)
    {
        _otherEnumValue = otherEnumValue;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TEnum> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        // Convert both to their underlying integral types
        var valueAsInt = Convert.ToInt64(value);
        var otherAsInt = Convert.ToInt64(_otherEnumValue);

        if (valueAsInt == otherAsInt)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value {valueAsInt} does not equal {otherAsInt}"));
    }

    protected override string GetExpectation() => $"to have the same value as {_otherEnumValue}";
}

/// <summary>
/// Asserts that two enum values do NOT have the same name.
/// </summary>
public class DoesNotHaveSameNameAsAssertion<TEnum> : Assertion<TEnum>
    where TEnum : struct, Enum
{
    private readonly Enum _otherEnumValue;

    public DoesNotHaveSameNameAsAssertion(
        AssertionContext<TEnum> context,
        Enum otherEnumValue)
        : base(context)
    {
        _otherEnumValue = otherEnumValue;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TEnum> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        var valueName = value.ToString();
        var otherName = _otherEnumValue.ToString();

        if (valueName != otherName)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value name \"{valueName}\" equals \"{otherName}\""));
    }

    protected override string GetExpectation() => $"to not have the same name as {_otherEnumValue}";
}

/// <summary>
/// Asserts that two enum values do NOT have the same underlying value.
/// </summary>
public class DoesNotHaveSameValueAsAssertion<TEnum> : Assertion<TEnum>
    where TEnum : struct, Enum
{
    private readonly Enum _otherEnumValue;

    public DoesNotHaveSameValueAsAssertion(
        AssertionContext<TEnum> context,
        Enum otherEnumValue)
        : base(context)
    {
        _otherEnumValue = otherEnumValue;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TEnum> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        var valueAsInt = Convert.ToInt64(value);
        var otherAsInt = Convert.ToInt64(_otherEnumValue);

        if (valueAsInt != otherAsInt)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value {valueAsInt} equals {otherAsInt}"));
    }

    protected override string GetExpectation() => $"to not have the same value as {_otherEnumValue}";
}
