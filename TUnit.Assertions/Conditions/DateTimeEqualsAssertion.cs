using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a DateTime is equal to an expected value.
/// Demonstrates custom methods WITHOUT wrappers: .Within() is directly on this class!
/// </summary>
public class DateTimeEqualsAssertion : Assertion<DateTime>
{
    private readonly DateTime _expected;
    private TimeSpan _tolerance = TimeSpan.Zero;

    public DateTimeEqualsAssertion(
        EvaluationContext<DateTime> context,
        DateTime expected,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _expected = expected;
    }

    /// <summary>
    /// ⚡ CUSTOM METHOD - No wrapper needed!
    /// Specifies the acceptable tolerance for the comparison.
    /// </summary>
    public DateTimeEqualsAssertion Within(TimeSpan tolerance)
    {
        _tolerance = tolerance;
        ExpressionBuilder.Append($".Within({tolerance})");
        return this; // Return self for continued chaining
    }

    protected override Task<AssertionResult> CheckAsync(DateTime value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var diff = Math.Abs((_expected - value).Ticks);

        if (diff <= _tolerance.Ticks)
            return Task.FromResult(AssertionResult.Passed);

        var actualDiff = TimeSpan.FromTicks(diff);
        return Task.FromResult(AssertionResult.Failed($"difference was {actualDiff}"));
    }

    protected override string GetExpectation() =>
        _tolerance == TimeSpan.Zero
            ? $"to be equal to {_expected}"
            : $"to be equal to {_expected} within {_tolerance}";
}
