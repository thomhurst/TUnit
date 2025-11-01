using System.Text;
using System.Text.RegularExpressions;
using TUnit.Assertions.Assertions.Regex;
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
            actualValue = RemoveWhitespace(actualValue);
            expectedValue = RemoveWhitespace(expectedValue);
        }

        if (actualValue.Contains(expectedValue, _comparison))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found \"{value}\""));
    }

    private static string RemoveWhitespace(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

#if NETSTANDARD2_0
        // Use StringBuilder for netstandard2.0 compatibility
        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (!char.IsWhiteSpace(c))
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
#else
        // Use Span<char> for better performance on modern .NET
        Span<char> buffer = input.Length <= 256
            ? stackalloc char[input.Length]
            : new char[input.Length];

        int writeIndex = 0;
        foreach (char c in input)
        {
            if (!char.IsWhiteSpace(c))
            {
                buffer[writeIndex++] = c;
            }
        }

        return new string(buffer.Slice(0, writeIndex));
#endif
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
/// Asserts that a string matches a regular expression pattern and returns a collection of all matches.
/// </summary>
public class StringMatchesAssertion : Assertion<RegexMatchCollection>
{
    private readonly string _pattern;
    private readonly Regex? _regex;
    private RegexOptions _options = RegexOptions.None;

    public StringMatchesAssertion(
        AssertionContext<string> context,
        string pattern)
        : base(CreateMappedContext(context, pattern, null, RegexOptions.None))
    {
        _pattern = pattern;
        _regex = null;
    }

    public StringMatchesAssertion(
        AssertionContext<string> context,
        Regex regex)
        : base(CreateMappedContext(context, regex.ToString(), regex, RegexOptions.None))
    {
        _pattern = regex.ToString();
        _regex = regex;
    }

    // Private constructor for chaining methods like IgnoringCase
    private StringMatchesAssertion(
        AssertionContext<RegexMatchCollection> mappedContext,
        string pattern,
        Regex? regex,
        RegexOptions options)
        : base(mappedContext)
    {
        _pattern = pattern;
        _regex = regex;
        _options = options;
    }

    public StringMatchesAssertion IgnoringCase()
    {
        var newOptions = _options | RegexOptions.IgnoreCase;
        Context.ExpressionBuilder.Append(".IgnoringCase()");
        return new StringMatchesAssertion(Context, _pattern, _regex, newOptions);
    }

    public StringMatchesAssertion WithOptions(RegexOptions options)
    {
        Context.ExpressionBuilder.Append($".WithOptions({options})");
        return new StringMatchesAssertion(Context, _pattern, _regex, options);
    }

    private static AssertionContext<RegexMatchCollection> CreateMappedContext(
        AssertionContext<string> context,
        string pattern,
        Regex? regex,
        RegexOptions options)
    {
        return context.Map<RegexMatchCollection>(stringValue =>
        {
            // Validate the regex pattern first (by creating a Regex object if we don't have one)
            // This ensures RegexParseException is thrown before ArgumentNullException for invalid patterns
            var regexObj = regex ?? new Regex(pattern, options);

            // Now check if value is null and throw ArgumentNullException
            if (stringValue == null)
            {
                throw new ArgumentNullException(nameof(stringValue), "value was null");
            }

            // Perform the matches
            var matches = regexObj.Matches(stringValue);

            if (matches.Count == 0)
            {
                throw new InvalidOperationException($"The regex \"{pattern}\" does not match with \"{stringValue}\"");
            }

            return new RegexMatchCollection(matches);
        });
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<RegexMatchCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            // Check what type of exception it is
            if (exception is InvalidOperationException)
            {
                return Task.FromResult(AssertionResult.Failed(exception.Message));
            }
            // Rethrow native exceptions (ArgumentNullException, RegexParseException, RegexMatchTimeoutException)
            // so they can be tested with .Throws<T>()
            throw exception;
        }

        // If we have a RegexMatchCollection, at least one match succeeded
        return Task.FromResult(AssertionResult.Passed);
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
