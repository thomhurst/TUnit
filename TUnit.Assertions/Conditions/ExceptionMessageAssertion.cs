using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that an exception message contains a specific substring.
/// Works on AndContinuation after Throws assertions.
/// </summary>
public class ExceptionMessageAssertion : Assertion<object?>
{
    private readonly string _expectedSubstring;
    private readonly StringComparison _comparison;

    public ExceptionMessageAssertion(
        EvaluationContext<object?> context,
        string expectedSubstring,
        StringBuilder expressionBuilder,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context, expressionBuilder)
    {
        _expectedSubstring = expectedSubstring;
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(object? value, Exception? exception)
    {
        if (exception == null)
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));

        if (exception.Message == null)
            return Task.FromResult(AssertionResult.Failed("exception message was null"));

        if (exception.Message.Contains(_expectedSubstring, _comparison))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"exception message was \"{exception.Message}\""));
    }

    protected override string GetExpectation() =>
        $"exception message to contain \"{_expectedSubstring}\"";
}
