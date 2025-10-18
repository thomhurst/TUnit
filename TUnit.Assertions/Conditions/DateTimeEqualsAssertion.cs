using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a DateTime is equal to an expected value.
/// Demonstrates custom methods WITHOUT wrappers: .Within() is directly on this class!
/// </summary>
[AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
public class DateTimeEqualsAssertion : ToleranceBasedEqualsAssertion<DateTime, TimeSpan>
{
    public DateTimeEqualsAssertion(
        AssertionContext<DateTime> context,
        DateTime expected)
        : base(context, expected)
    {
    }

    protected override bool HasToleranceValue()
    {
        // For DateTime, we use TimeSpan.Zero as the default, so check if it's not zero
        return true; // TimeSpan? always has meaningful value when not null
    }

    protected override bool IsWithinTolerance(DateTime actual, DateTime expected, TimeSpan tolerance)
    {
        var diff = Math.Abs((expected - actual).Ticks);
        return diff <= tolerance.Ticks;
    }

    protected override object CalculateDifference(DateTime actual, DateTime expected)
    {
        var diff = Math.Abs((expected - actual).Ticks);
        return TimeSpan.FromTicks(diff);
    }

    protected override bool AreExactlyEqual(DateTime actual, DateTime expected)
    {
        return actual == expected;
    }

    protected override string FormatDifferenceMessage(DateTime actual, object difference)
    {
        return $"difference was {difference}";
    }

    protected override string GetExpectation()
    {
        // Override to use "equal to" instead of just tolerance message
        var baseExpectation = base.GetExpectation();
        return baseExpectation.Replace("to be within", "to be equal to expected within").Replace(" of ", " ");
    }
}
