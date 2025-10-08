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
        EvaluationContext<string> context,
        string expected,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
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
        ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Specifies a custom string comparison type.
    /// </summary>
    public StringEqualsAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Trims both strings before comparing.
    /// </summary>
    public StringEqualsAssertion WithTrimming()
    {
        _trimming = true;
        ExpressionBuilder.Append(".WithTrimming()");
        return this;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Treats null and empty string as equal.
    /// </summary>
    public StringEqualsAssertion WithNullAndEmptyEquality()
    {
        _nullAndEmptyEquality = true;
        ExpressionBuilder.Append(".WithNullAndEmptyEquality()");
        return this;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Removes all whitespace from both strings before comparing.
    /// </summary>
    public StringEqualsAssertion IgnoringWhitespace()
    {
        _ignoringWhitespace = true;
        ExpressionBuilder.Append(".IgnoringWhitespace()");
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
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation()
    {
        var comparisonDesc = _comparison == StringComparison.Ordinal
            ? ""
            : $" ({_comparison})";
        return $"to be equal to \"{_expected}\"{comparisonDesc}";
    }
}
