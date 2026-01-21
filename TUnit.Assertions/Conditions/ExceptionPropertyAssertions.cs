using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that an exception's Message property contains a specific substring.
/// </summary>
public class ExceptionMessageContainsAssertion<TException> : Assertion<TException>
    where TException : Exception
{
    private readonly string _expectedSubstring;
    private readonly StringComparison _comparison;

    public ExceptionMessageContainsAssertion(
        AssertionContext<TException> context,
        string expectedSubstring,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context)
    {
        _expectedSubstring = expectedSubstring;
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
    {
        var exception = metadata.Value;
        var evaluationException = metadata.Exception;

        if (evaluationException != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {evaluationException.GetType().FullName}"));
        }

        if (exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));
        }

        if (exception.Message == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception message was null"));
        }

        if (exception.Message.Contains(_expectedSubstring, _comparison))
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"exception message was \"{exception.Message}\""));
    }

    protected override string GetExpectation() =>
        $"exception message to contain \"{_expectedSubstring}\"";
}

/// <summary>
/// Asserts that an exception's Message property exactly equals a specific string.
/// </summary>
public class ExceptionMessageEqualsAssertion<TException> : Assertion<TException>
    where TException : Exception
{
    private readonly string _expectedMessage;
    private readonly StringComparison _comparison;

    public ExceptionMessageEqualsAssertion(
        AssertionContext<TException> context,
        string expectedMessage,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context)
    {
        _expectedMessage = expectedMessage;
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
    {
        var exception = metadata.Value;
        var evaluationException = metadata.Exception;

        if (evaluationException != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {evaluationException.GetType().FullName}"));
        }

        if (exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));
        }

        if (string.Equals(exception.Message, _expectedMessage, _comparison))
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed($"exception message was \"{exception.Message}\""));
    }

    protected override string GetExpectation() =>
        $"exception message to equal \"{_expectedMessage}\"";
}

/// <summary>
/// Asserts that an exception's Message property does NOT contain a specific substring.
/// </summary>
public class ExceptionMessageNotContainsAssertion<TException> : Assertion<TException>
    where TException : Exception
{
    private readonly string _notExpectedSubstring;
    private readonly StringComparison _comparison;

    public ExceptionMessageNotContainsAssertion(
        AssertionContext<TException> context,
        string notExpectedSubstring,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context)
    {
        _notExpectedSubstring = notExpectedSubstring;
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
    {
        var exception = metadata.Value;
        var evaluationException = metadata.Exception;

        if (evaluationException != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {evaluationException.GetType().FullName}"));
        }

        if (exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));
        }

        if (exception.Message != null && exception.Message.Contains(_notExpectedSubstring, _comparison))
        {
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{exception.Message}\" should not contain \"{_notExpectedSubstring}\""));
        }

        return AssertionResult._passedTask;
    }

    protected override string GetExpectation() =>
        $"exception message to not contain \"{_notExpectedSubstring}\"";
}

/// <summary>
/// Asserts that an exception's Message property matches a wildcard pattern.
/// * matches any number of characters, ? matches a single character.
/// </summary>
public class ExceptionMessageMatchesPatternAssertion<TException> : Assertion<TException>
    where TException : Exception
{
    private readonly string _pattern;

    public ExceptionMessageMatchesPatternAssertion(
        AssertionContext<TException> context,
        string pattern)
        : base(context)
    {
        _pattern = pattern;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
    {
        var exception = metadata.Value;
        var evaluationException = metadata.Exception;

        if (evaluationException != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {evaluationException.GetType().FullName}"));
        }

        if (exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));
        }

        if (exception.Message == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception message was null"));
        }

        if (MatchesPattern(exception.Message, _pattern))
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed(
            $"exception message \"{exception.Message}\" does not match pattern \"{_pattern}\""));
    }

    protected override string GetExpectation() =>
        $"exception message to match pattern \"{_pattern}\"";

    private static bool MatchesPattern(string input, string pattern)
    {
        // Convert wildcard pattern to regex
        // * matches any number of characters (including newlines)
        // ? matches a single character
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        // Use Singleline option so . matches newlines (needed for multiline error messages)
        return Regex.IsMatch(input, regexPattern, RegexOptions.Singleline);
    }
}

/// <summary>
/// Asserts that an exception's Message property matches a StringMatcher pattern.
/// </summary>
public class ExceptionMessageMatchesAssertion<TException> : Assertion<TException>
    where TException : Exception
{
    private readonly StringMatcher _matcher;

    public ExceptionMessageMatchesAssertion(
        AssertionContext<TException> context,
        StringMatcher matcher)
        : base(context)
    {
        _matcher = matcher;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
    {
        var exception = metadata.Value;
        var evaluationException = metadata.Exception;

        if (evaluationException != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {evaluationException.GetType().FullName}"));
        }

        if (exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));
        }

        if (exception.Message == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception message was null"));
        }

        if (_matcher.IsMatch(exception.Message))
        {
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed(
            $"exception message \"{exception.Message}\" does not match {_matcher}"));
    }

    protected override string GetExpectation() =>
        $"exception message to match {_matcher}";
}

/// <summary>
/// Asserts that an ArgumentException has a specific parameter name.
/// </summary>
public class ExceptionParameterNameAssertion<TException> : Assertion<TException>
    where TException : Exception
{
    private readonly string _expectedParameterName;
    private readonly bool _requireExactType;

    public ExceptionParameterNameAssertion(
        AssertionContext<TException> context,
        string expectedParameterName,
        bool requireExactType = false)
        : base(context)
    {
        _expectedParameterName = expectedParameterName;
        _requireExactType = requireExactType;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
    {
        var exception = metadata.Value;
        var evaluationException = metadata.Exception;

        if (evaluationException != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {evaluationException.GetType().FullName}"));
        }

        if (exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));
        }

        // If exact type is required, check that first
        if (_requireExactType && exception.GetType() != typeof(TException))
        {
            return Task.FromResult(AssertionResult.Failed(
                $"wrong exception type: {exception.GetType().Name} instead of exactly {typeof(TException).Name}"));
        }

        if (exception is ArgumentException argumentException)
        {
            if (argumentException.ParamName == _expectedParameterName)
            {
                return AssertionResult._passedTask;
            }

            return Task.FromResult(AssertionResult.Failed(
                $"ArgumentException parameter name was \"{argumentException.ParamName}\""));
        }

        return Task.FromResult(AssertionResult.Failed(
            $"WithParameterName can only be used with ArgumentException, but exception is {exception.GetType().Name}"));
    }

    protected override string GetExpectation() =>
        _requireExactType
            ? $"{typeof(TException).Name} to have parameter name \"{_expectedParameterName}\" (exact type)"
            : $"ArgumentException to have parameter name \"{_expectedParameterName}\"";
}
