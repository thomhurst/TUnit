using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a string is equal to an expected value.
/// Demonstrates multiple custom methods WITHOUT wrappers!
/// </summary>
public class StringEqualsAssertion : Assertion<string>
{
    private readonly string _expected;
    private StringComparison _comparison = StringComparison.Ordinal;
    private bool _trimming = false;
    private bool _nullAndEmptyEquality = false;
    private bool _ignoringWhitespace = false;

    public StringEqualsAssertion(
        AssertionContext<string> context,
        string expected)
        : base(context)
    {
        _expected = expected;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Makes the comparison case-insensitive.
    /// </summary>
    public StringEqualsAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        Context.ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Specifies a custom string comparison type.
    /// </summary>
    public StringEqualsAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        Context.ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Trims both strings before comparing.
    /// </summary>
    public StringEqualsAssertion WithTrimming()
    {
        _trimming = true;
        Context.ExpressionBuilder.Append(".WithTrimming()");
        return this;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Treats null and empty string as equal.
    /// </summary>
    public StringEqualsAssertion WithNullAndEmptyEquality()
    {
        _nullAndEmptyEquality = true;
        Context.ExpressionBuilder.Append(".WithNullAndEmptyEquality()");
        return this;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Removes all whitespace from both strings before comparing.
    /// </summary>
    public StringEqualsAssertion IgnoringWhitespace()
    {
        _ignoringWhitespace = true;
        Context.ExpressionBuilder.Append(".IgnoringWhitespace()");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        var actualValue = value;
        var expectedValue = _expected;

        if (_trimming)
        {
            actualValue = actualValue?.Trim();
            expectedValue = expectedValue?.Trim();
        }

        if (_ignoringWhitespace)
        {
            actualValue = actualValue != null ? string.Concat(actualValue.Where(c => !char.IsWhiteSpace(c))) : null;
            expectedValue = expectedValue != null ? string.Concat(expectedValue.Where(c => !char.IsWhiteSpace(c))) : null;
        }

        if (_nullAndEmptyEquality)
        {
            actualValue = string.IsNullOrEmpty(actualValue) ? null : actualValue;
            expectedValue = string.IsNullOrEmpty(expectedValue) ? null : expectedValue;
        }

        if (string.Equals(actualValue, expectedValue, _comparison))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        // Build detailed error message for string comparison failures
        var errorMessage = BuildStringDifferenceMessage(value, actualValue, expectedValue);
        return Task.FromResult(AssertionResult.Failed(errorMessage));
    }

    private string BuildStringDifferenceMessage(string? originalValue, string? actualValue, string? expectedValue)
    {
        // For simple cases, just show the value
        if (actualValue == null || expectedValue == null ||
            (actualValue.Length <= 100 && expectedValue.Length <= 100))
        {
            return $"found \"{originalValue}\"";
        }

        // Find the first index where the strings differ
        int diffIndex = -1;
        int minLength = Math.Min(actualValue.Length, expectedValue.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (!CharEquals(actualValue[i], expectedValue[i]))
            {
                diffIndex = i;
                break;
            }
        }

        // If no difference found in common length, the difference is at the end
        if (diffIndex == -1 && actualValue.Length != expectedValue.Length)
        {
            diffIndex = minLength;
        }

        // If still no difference found, just show the value (shouldn't happen)
        if (diffIndex == -1)
        {
            return $"found \"{originalValue}\"";
        }

        // Build the message with truncation and diff display
        var message = new StringBuilder();
        message.Append($"found \"{TruncateString(originalValue, 99)}\" which differs at index {diffIndex}:");
        message.AppendLine();

        // Show context around the difference (about 24 chars before and 26 after)
        int contextStart = Math.Max(0, diffIndex - 24);
        int contextEnd = Math.Min(expectedValue.Length, Math.Min(actualValue.Length, diffIndex + 27));

        // Build the diff display with arrows
        var expectedContext = expectedValue.Substring(contextStart, Math.Min(contextEnd - contextStart, expectedValue.Length - contextStart));
        var actualContext = actualValue.Substring(contextStart, Math.Min(contextEnd - contextStart, actualValue.Length - contextStart));

        // Calculate arrow position (relative to context start + prefix + opening quote)
        int arrowPosition = diffIndex - contextStart;
        string arrow = new string(' ', arrowPosition + 4); // +3 for "   " prefix, +1 for opening quote

        message.AppendLine($"{arrow}↓");
        message.AppendLine($"   \"{TruncateString(actualContext, 50)}\"");
        message.AppendLine($"   \"{TruncateString(expectedContext, 50)}\"");
        message.Append($"{arrow}↑");

        return message.ToString();
    }

    private bool CharEquals(char a, char b)
    {
        if (_comparison == StringComparison.Ordinal || _comparison == StringComparison.InvariantCulture)
        {
            return a == b;
        }

        if (_comparison == StringComparison.OrdinalIgnoreCase || _comparison == StringComparison.InvariantCultureIgnoreCase)
        {
            return char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
        }

        return string.Compare(a.ToString(), b.ToString(), _comparison) == 0;
    }

    private static string TruncateString(string? str, int maxLength)
    {
        if (str == null || str.Length <= maxLength)
        {
            return str ?? "";
        }

        return str.Substring(0, maxLength) + "…";
    }

    protected override string GetExpectation()
    {
        var comparisonDesc = _comparison == StringComparison.Ordinal
            ? ""
            : $" ({_comparison})";
        var truncatedExpected = TruncateString(_expected, 99);
        return $"to be equal to \"{truncatedExpected}\"{comparisonDesc}";
    }
}
