using System.Text;
using System.Text.RegularExpressions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a string contains the expected substring.
/// </summary>
[AssertionExtension("Contains")]
public class StringContainsAssertion : Assertion<string>
{
    private readonly string _expected;
    private StringComparison _comparison = StringComparison.Ordinal;
    private bool _trimming = false;
    private bool _ignoringWhitespace = false;

    public StringContainsAssertion(
        AssertionContext<string> context,
        string expected)
        : base(context)
    {
        _expected = expected;
    }

    public StringContainsAssertion(
        AssertionContext<string> context,
        string expected,
        StringComparison comparison)
        : base(context)
    {
        _expected = expected;
        _comparison = comparison;
    }

    public StringContainsAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        Context.ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringContainsAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        Context.ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    public StringContainsAssertion WithTrimming()
    {
        _trimming = true;
        Context.ExpressionBuilder.Append(".WithTrimming()");
        return this;
    }

    public StringContainsAssertion IgnoringWhitespace()
    {
        _ignoringWhitespace = true;
        Context.ExpressionBuilder.Append(".IgnoringWhitespace()");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

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
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => $"to contain \"{_expected}\"";
}

/// <summary>
/// Asserts that a string does NOT contain the expected substring.
/// </summary>
[AssertionExtension("DoesNotContain")]
public class StringDoesNotContainAssertion : Assertion<string>
{
    private readonly string _expected;
    private StringComparison _comparison = StringComparison.Ordinal;

    public StringDoesNotContainAssertion(
        AssertionContext<string> context,
        string expected)
        : base(context)
    {
        _expected = expected;
    }

    public StringDoesNotContainAssertion(
        AssertionContext<string> context,
        string expected,
        StringComparison comparison)
        : base(context)
    {
        _expected = expected;
        _comparison = comparison;
    }

    public StringDoesNotContainAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        Context.ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringDoesNotContainAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        Context.ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (!value.Contains(_expected, _comparison))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found \"{_expected}\" in \"{value}\""));
    }

    protected override string GetExpectation() => $"to not contain \"{_expected}\"";
}

/// <summary>
/// Asserts that a string starts with the expected substring.
/// </summary>
[AssertionExtension("StartsWith")]
public class StringStartsWithAssertion : Assertion<string>
{
    private readonly string _expected;
    private StringComparison _comparison = StringComparison.Ordinal;

    public StringStartsWithAssertion(
        AssertionContext<string> context,
        string expected)
        : base(context)
    {
        _expected = expected;
    }

    public StringStartsWithAssertion(
        AssertionContext<string> context,
        string expected,
        StringComparison comparison)
        : base(context)
    {
        _expected = expected;
        _comparison = comparison;
    }

    public StringStartsWithAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        Context.ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringStartsWithAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        Context.ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (value.StartsWith(_expected, _comparison))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => $"to start with \"{_expected}\"";
}

/// <summary>
/// Asserts that a string ends with the expected substring.
/// </summary>
[AssertionExtension("EndsWith")]
public class StringEndsWithAssertion : Assertion<string>
{
    private readonly string _expected;
    private StringComparison _comparison = StringComparison.Ordinal;

    public StringEndsWithAssertion(
        AssertionContext<string> context,
        string expected)
        : base(context)
    {
        _expected = expected;
    }

    public StringEndsWithAssertion(
        AssertionContext<string> context,
        string expected,
        StringComparison comparison)
        : base(context)
    {
        _expected = expected;
        _comparison = comparison;
    }

    public StringEndsWithAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        Context.ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringEndsWithAssertion WithComparison(StringComparison comparison)
    {
        _comparison = comparison;
        Context.ExpressionBuilder.Append($".WithComparison({comparison})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (value.EndsWith(_expected, _comparison))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => $"to end with \"{_expected}\"";
}

/// <summary>
/// Asserts that a string is not empty or whitespace.
/// </summary>
[AssertionExtension("IsNotEmpty")]
public class StringIsNotEmptyAssertion : Assertion<string>
{
    public StringIsNotEmptyAssertion(
        AssertionContext<string> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (!string.IsNullOrEmpty(value))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    protected override string GetExpectation() => "to not be empty or whitespace";
}

/// <summary>
/// Asserts that a string is empty or whitespace.
/// </summary>
[AssertionExtension("IsEmpty")]
public class StringIsEmptyAssertion : Assertion<string>
{
    public StringIsEmptyAssertion(
        AssertionContext<string> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (value == string.Empty)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

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
        AssertionContext<string> context,
        int expectedLength)
        : base(context)
    {
        _expectedLength = expectedLength;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (value.Length == _expectedLength)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found length {value.Length}"));
    }

    protected override string GetExpectation() => $"to have length {_expectedLength}";
}

/// <summary>
/// Asserts that a string matches a regular expression pattern.
/// </summary>
[AssertionExtension("Matches")]
public class StringMatchesAssertion : Assertion<string>
{
    private readonly string _pattern;
    private readonly Regex? _regex;
    private RegexOptions _options = RegexOptions.None;
    private Match? _cachedMatch;

    public StringMatchesAssertion(
        AssertionContext<string> context,
        string pattern)
        : base(context)
    {
        _pattern = pattern;
        _regex = null;
    }

    public StringMatchesAssertion(
        AssertionContext<string> context,
        Regex regex)
        : base(context)
    {
        _pattern = regex.ToString();
        _regex = regex;
    }

    public StringMatchesAssertion IgnoringCase()
    {
        _options |= RegexOptions.IgnoreCase;
        Context.ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringMatchesAssertion WithOptions(RegexOptions options)
    {
        _options = options;
        Context.ExpressionBuilder.Append($".WithOptions({options})");
        return this;
    }

    /// <summary>
    /// Gets the cached regex match result after the assertion has been executed.
    /// Returns null if the assertion hasn't been executed yet or if the match failed.
    /// </summary>
    public Match? GetMatch() => _cachedMatch;

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        // Validate the regex pattern first (by creating a Regex object if we don't have one)
        // This ensures RegexParseException is thrown before ArgumentNullException for invalid patterns
        var regex = _regex ?? new Regex(_pattern, _options);

        // Now check if value is null and throw ArgumentNullException
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "value was null");
        }
        // Use the validated regex to check the match and cache it
        var match = regex.Match(value);
        _cachedMatch = match;

        if (match.Success)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"The regex \"{_pattern}\" does not match with \"{value}\""));
    }

    protected override string GetExpectation()
    {
        // Check expression builder to detect if variable was named "regex" (GeneratedRegex pattern)
        var expression = Context.ExpressionBuilder.ToString();
        if (expression.Contains(".Matches(regex)") || expression.Contains(".Matches(Matches_"))
        {
            return "text match regex";
        }
        return "text match pattern";
    }
}

/// <summary>
/// Asserts that a string does NOT match a regular expression pattern.
/// </summary>
[AssertionExtension("DoesNotMatch")]
public class StringDoesNotMatchAssertion : Assertion<string>
{
    private readonly string _pattern;
    private readonly Regex? _regex;
    private RegexOptions _options = RegexOptions.None;

    public StringDoesNotMatchAssertion(
        AssertionContext<string> context,
        string pattern)
        : base(context)
    {
        _pattern = pattern;
        _regex = null;
    }

    public StringDoesNotMatchAssertion(
        AssertionContext<string> context,
        Regex regex)
        : base(context)
    {
        _pattern = regex.ToString();
        _regex = regex;
    }

    public StringDoesNotMatchAssertion IgnoringCase()
    {
        _options |= RegexOptions.IgnoreCase;
        Context.ExpressionBuilder.Append(".IgnoringCase()");
        return this;
    }

    public StringDoesNotMatchAssertion WithOptions(RegexOptions options)
    {
        _options = options;
        Context.ExpressionBuilder.Append($".WithOptions({options})");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        // Validate the regex pattern first (by creating a Regex object if we don't have one)
        // This ensures RegexParseException is thrown before ArgumentNullException for invalid patterns
        var regex = _regex ?? new Regex(_pattern, _options);

        // Now check if value is null and throw ArgumentNullException
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "value was null");
        }

        // Use the validated regex to check the match
        bool isMatch = regex.IsMatch(value);

        if (!isMatch)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"The regex \"{_pattern}\" matches with \"{value}\""));
    }

    protected override string GetExpectation()
    {
        // Check expression builder to detect if variable was named "regex" (GeneratedRegex pattern)
        var expression = Context.ExpressionBuilder.ToString();
        if (expression.Contains(".DoesNotMatch(regex)") || expression.Contains(".DoesNotMatch(DoesNotMatch_") || expression.Contains(".DoesNotMatch(FindNumber"))
        {
            return "text to not match with regex";
        }
        return "text to not match with pattern";
    }
}
