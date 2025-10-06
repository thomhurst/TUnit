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

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (string.Equals(value, _expected, _comparison))
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
