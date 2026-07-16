using System.ComponentModel;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Assertions.Enums;

/// <summary>
/// Source-generated enum assertions using [GenerateAssertion] attributes.
/// </summary>
public static partial class EnumAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have flag {expectedFlag}")]
    public static AssertionResult HasFlag<TEnum>(this TEnum value, TEnum expectedFlag)
        where TEnum : struct, Enum
    {
        return ((Enum)value).HasFlag((Enum)expectedFlag)
            ? AssertionResult.Passed
            : AssertionResult.Failed($"value {value} does not have flag {expectedFlag}");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not have flag {unexpectedFlag}")]
    public static AssertionResult DoesNotHaveFlag<TEnum>(this TEnum value, TEnum unexpectedFlag)
        where TEnum : struct, Enum
    {
        return !((Enum)value).HasFlag((Enum)unexpectedFlag)
            ? AssertionResult.Passed
            : AssertionResult.Failed($"value {value} has flag {unexpectedFlag}");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have the same name as {otherEnumValue}")]
    public static AssertionResult HasSameNameAs<TEnum>(this TEnum value, Enum otherEnumValue)
        where TEnum : struct, Enum
    {
        var valueName = value.ToString();
        var otherName = otherEnumValue.ToString();

        return valueName == otherName
            ? AssertionResult.Passed
            : AssertionResult.Failed($"value name \"{valueName}\" does not equal \"{otherName}\"");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not have the same name as {otherEnumValue}")]
    public static AssertionResult DoesNotHaveSameNameAs<TEnum>(this TEnum value, Enum otherEnumValue)
        where TEnum : struct, Enum
    {
        var valueName = value.ToString();
        var otherName = otherEnumValue.ToString();

        return valueName != otherName
            ? AssertionResult.Passed
            : AssertionResult.Failed($"value name \"{valueName}\" equals \"{otherName}\"");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have the same value as {otherEnumValue}")]
    public static AssertionResult HasSameValueAs<TEnum>(this TEnum value, Enum otherEnumValue)
        where TEnum : struct, Enum
    {
        var valueAsInt = Convert.ToInt64(value);
        var otherAsInt = Convert.ToInt64(otherEnumValue);

        return valueAsInt == otherAsInt
            ? AssertionResult.Passed
            : AssertionResult.Failed($"value {valueAsInt} does not equal {otherAsInt}");
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not have the same value as {otherEnumValue}")]
    public static AssertionResult DoesNotHaveSameValueAs<TEnum>(this TEnum value, Enum otherEnumValue)
        where TEnum : struct, Enum
    {
        var valueAsInt = Convert.ToInt64(value);
        var otherAsInt = Convert.ToInt64(otherEnumValue);

        return valueAsInt != otherAsInt
            ? AssertionResult.Passed
            : AssertionResult.Failed($"value {valueAsInt} equals {otherAsInt}");
    }
}
