using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that the DateTime represents today's date.
/// </summary>
[AssertionExtension("IsToday")]
public class IsTodayAssertion : Assertion<DateTime>
{
    public IsTodayAssertion(AssertionContext<DateTime> context) : base(context)
    {
    }

    protected override string GetExpectation() => "to be today";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTime> metadata)
    {
        var actualDate = metadata.Value.Date;
        var today = DateTime.Today;

        if (actualDate == today)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to be today ({today:yyyy-MM-dd}), but it was {actualDate:yyyy-MM-dd}"));
    }
}

/// <summary>
/// Asserts that the DateTime does not represent today's date.
/// </summary>
[AssertionExtension("IsNotToday")]
public class IsNotTodayAssertion : Assertion<DateTime>
{
    public IsNotTodayAssertion(AssertionContext<DateTime> context) : base(context)
    {
    }

    protected override string GetExpectation() => "to not be today";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTime> metadata)
    {
        var actualDate = metadata.Value.Date;
        var today = DateTime.Today;

        if (actualDate != today)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to not be today, but it was {today:yyyy-MM-dd}"));
    }
}

/// <summary>
/// Asserts that the DateTime is in UTC.
/// </summary>
[AssertionExtension("IsUtc")]
public class IsUtcAssertion : Assertion<DateTime>
{
    public IsUtcAssertion(AssertionContext<DateTime> context) : base(context)
    {
    }

    protected override string GetExpectation() => "to be UTC";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTime> metadata)
    {
        if (metadata.Value.Kind == DateTimeKind.Utc)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to be UTC, but it was {metadata.Value.Kind}"));
    }
}

/// <summary>
/// Asserts that the DateTime is not in UTC.
/// </summary>
[AssertionExtension("IsNotUtc")]
public class IsNotUtcAssertion : Assertion<DateTime>
{
    public IsNotUtcAssertion(AssertionContext<DateTime> context) : base(context)
    {
    }

    protected override string GetExpectation() => "to not be UTC";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTime> metadata)
    {
        if (metadata.Value.Kind != DateTimeKind.Utc)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to not be UTC, but it was UTC"));
    }
}

/// <summary>
/// Asserts that the DateTime is in a leap year.
/// </summary>
[AssertionExtension("IsLeapYear")]
public class IsLeapYearAssertion : Assertion<DateTime>
{
    public IsLeapYearAssertion(AssertionContext<DateTime> context) : base(context)
    {
    }

    protected override string GetExpectation() => "to be in a leap year";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTime> metadata)
    {
        if (DateTime.IsLeapYear(metadata.Value.Year))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to be in a leap year, but {metadata.Value.Year} is not a leap year"));
    }
}

/// <summary>
/// Asserts that the DateTime is not in a leap year.
/// </summary>
[AssertionExtension("IsNotLeapYear")]
public class IsNotLeapYearAssertion : Assertion<DateTime>
{
    public IsNotLeapYearAssertion(AssertionContext<DateTime> context) : base(context)
    {
    }

    protected override string GetExpectation() => "to not be in a leap year";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTime> metadata)
    {
        if (!DateTime.IsLeapYear(metadata.Value.Year))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to not be in a leap year, but {metadata.Value.Year} is a leap year"));
    }
}

/// <summary>
/// Asserts that the DateTime is during daylight saving time.
/// </summary>
[AssertionExtension("IsDaylightSavingTime")]
public class IsDaylightSavingTimeAssertion : Assertion<DateTime>
{
    public IsDaylightSavingTimeAssertion(AssertionContext<DateTime> context) : base(context)
    {
    }

    protected override string GetExpectation() => "to be during daylight saving time";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTime> metadata)
    {
        if (metadata.Value.IsDaylightSavingTime())
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to be during daylight saving time, but it was not"));
    }
}

/// <summary>
/// Asserts that the DateTime is not during daylight saving time.
/// </summary>
[AssertionExtension("IsNotDaylightSavingTime")]
public class IsNotDaylightSavingTimeAssertion : Assertion<DateTime>
{
    public IsNotDaylightSavingTimeAssertion(AssertionContext<DateTime> context) : base(context)
    {
    }

    protected override string GetExpectation() => "to not be during daylight saving time";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTime> metadata)
    {
        if (!metadata.Value.IsDaylightSavingTime())
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to not be during daylight saving time, but it was"));
    }
}

/// <summary>
/// Asserts exact DateTime equality (including ticks).
/// </summary>
[AssertionExtension("EqualsExact")]
public class DateTimeEqualsExactAssertion : Assertion<DateTime>
{
    private readonly DateTime _expected;

    public DateTimeEqualsExactAssertion(AssertionContext<DateTime> context, DateTime expected) : base(context)
    {
        _expected = expected;
    }

    protected override string GetExpectation() => $"to exactly equal {_expected:O}";

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DateTime> metadata)
    {
        if (metadata.Value == _expected)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to exactly equal {_expected:O}, but it was {metadata.Value:O}"));
    }
}
