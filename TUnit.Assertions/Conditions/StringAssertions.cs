using System.Text;
using System.Text.RegularExpressions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a string contains the expected substring.
/// </summary>
public class StringContainsAssertion : Assertion<string>
{
    private readonly string _expected;
    private StringComparison _comparison = StringComparison.Ordinal;
    private bool _trimming = false;
    private bool _ignoringWhitespace = false;

    public StringContainsAssertion(
        EvaluationContext<string> context,
        string expected,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expected = expected;
    }

    public StringContainsAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringContainsAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    public StringContainsAssertion WithTrimming()
    {
        _trimming = true;
        ExpressionBuilder.Append(".WithTrimming()");
        return this;
    }

    public StringContainsAssertion IgnoringWhitespace()
    {
        _ignoringWhitespace = true;
        ExpressionBuilder.Append(".IgnoringWhitespace()");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        var actualValue = value;
        var expectedValue = _expected;

        if (_trimming)
        {
            actualValue = actualValue.Trim();
            expectedValue = expectedValue.Trim();
        }

        if (_ignoringWhitespace)
        {
            actualValue = string.Concat(actualValue.Where(c => !char.IsWhiteSpace(c)));
            expectedValue = string.Concat(expectedValue.Where(c => !char.IsWhiteSpace(c)));
        }

        if (actualValue.Contains(expectedValue, _comparison))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => $"to contain \"{_expected}\"";
}

/// <summary>
/// Asserts that a string does NOT contain the expected substring.
/// </summary>
public class StringDoesNotContainAssertion : Assertion<string>
{
    private readonly string _expected;
    private StringComparison _comparison = StringComparison.Ordinal;

    public StringDoesNotContainAssertion(
        EvaluationContext<string> context,
        string expected,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expected = expected;
    }

    public StringDoesNotContainAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringDoesNotContainAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        if (!value.Contains(_expected, _comparison))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{_expected}\" in \"{value}\""));
    }

    protected override string GetExpectation() => $"to not contain \"{_expected}\"";
}

/// <summary>
/// Asserts that a string starts with the expected substring.
/// </summary>
public class StringStartsWithAssertion : Assertion<string>
{
    private readonly string _expected;
    private StringComparison _comparison = StringComparison.Ordinal;

    public StringStartsWithAssertion(
        EvaluationContext<string> context,
        string expected,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expected = expected;
    }

    public StringStartsWithAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringStartsWithAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        if (value.StartsWith(_expected, _comparison))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => $"to start with \"{_expected}\"";
}

/// <summary>
/// Asserts that a string ends with the expected substring.
/// </summary>
public class StringEndsWithAssertion : Assertion<string>
{
    private readonly string _expected;
    private StringComparison _comparison = StringComparison.Ordinal;

    public StringEndsWithAssertion(
        EvaluationContext<string> context,
        string expected,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expected = expected;
    }

    public StringEndsWithAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringEndsWithAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        if (value.EndsWith(_expected, _comparison))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => $"to end with \"{_expected}\"";
}

/// <summary>
/// Asserts that a string is not empty or whitespace.
/// </summary>
public class StringIsNotEmptyAssertion : Assertion<string>
{
    public StringIsNotEmptyAssertion(
        EvaluationContext<string> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        if (!string.IsNullOrWhiteSpace(value))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => "to not be empty or whitespace";
}

/// <summary>
/// Asserts that a string is empty or whitespace.
/// </summary>
public class StringIsEmptyAssertion : Assertion<string>
{
    public StringIsEmptyAssertion(
        EvaluationContext<string> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => "to be empty or whitespace";
}

/// <summary>
/// Asserts that a string has a specific length.
/// </summary>
public class StringLengthAssertion : Assertion<string>
{
    private readonly int _expectedLength;

    public StringLengthAssertion(
        EvaluationContext<string> context,
        int expectedLength,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expectedLength = expectedLength;
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        if (value.Length == _expectedLength)
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found length {value.Length}"));
    }

    protected override string GetExpectation() => $"to have length {_expectedLength}";
}

/// <summary>
/// Asserts that a string is null, empty, or whitespace.
/// </summary>
public class StringIsNullOrWhitespaceAssertion : Assertion<string>
{
    public StringIsNullOrWhitespaceAssertion(
        EvaluationContext<string> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => "to be null, empty, or whitespace";
}

/// <summary>
/// Asserts that a string matches a regular expression pattern.
/// </summary>
public class StringMatchesAssertion : Assertion<string>
{
    private readonly string _pattern;
    private RegexOptions _options = RegexOptions.None;

    public StringMatchesAssertion(
        EvaluationContext<string> context,
        string pattern,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _pattern = pattern;
    }

    public StringMatchesAssertion IgnoringCase()
    {
        _options |= RegexOptions.IgnoreCase;
        ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringMatchesAssertion WithOptions(RegexOptions options)
    {
        _options = options;
        ExpressionBuilder.Append($".WithOptions({options})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        if (Regex.IsMatch(value, _pattern, _options))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => $"to match pattern \"{_pattern}\"";
}

/// <summary>
/// Asserts that a string does NOT match a regular expression pattern.
/// </summary>
public class StringDoesNotMatchAssertion : Assertion<string>
{
    private readonly string _pattern;
    private RegexOptions _options = RegexOptions.None;

    public StringDoesNotMatchAssertion(
        EvaluationContext<string> context,
        string pattern,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _pattern = pattern;
    }

    public StringDoesNotMatchAssertion IgnoringCase()
    {
        _options |= RegexOptions.IgnoreCase;
        ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringDoesNotMatchAssertion WithOptions(RegexOptions options)
    {
        _options = options;
        ExpressionBuilder.Append($".WithOptions({options})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        if (!Regex.IsMatch(value, _pattern, _options))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\" matching pattern"));
    }

    protected override string GetExpectation() => $"to not match pattern \"{_pattern}\"";
}

/// <summary>
/// Asserts that a string is null or empty.
/// </summary>
public class StringIsNullOrEmptyAssertion : Assertion<string>
{
    public StringIsNullOrEmptyAssertion(
        EvaluationContext<string> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (string.IsNullOrEmpty(value))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => "to be null or empty";
}

/// <summary>
/// Asserts that a string is NOT null or empty.
/// </summary>
public class StringIsNotNullOrEmptyAssertion : Assertion<string>
{
    public StringIsNotNullOrEmptyAssertion(
        EvaluationContext<string> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(string? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (!string.IsNullOrEmpty(value))
            return Task.FromResult(AssertionResult.Passed);

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("value was null"));

        return Task.FromResult(AssertionResult.Failed("value was empty"));
    }

    protected override string GetExpectation() => "to not be null or empty";
}
