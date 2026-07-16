using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;
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
            return AssertionResult._passedTask;
        }

        return Task.FromResult(AssertionResult.Failed(
            $"Expected {Context.ExpressionBuilder} to exactly equal {_expected:O}, but it was {metadata.Value:O}"));
    }
}
