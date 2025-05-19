#if NET

namespace TUnit.Assertions.AssertConditions.Chronology;

public class TimeOnlyEqualsExpectedValueAssertCondition(TimeOnly expected) : ExpectedValueAssertCondition<TimeOnly, TimeOnly>(expected) 
{
    private TimeSpan? _tolerance;

    protected override string GetExpectation()
    {
        if (_tolerance == null || _tolerance == TimeSpan.Zero)
        {
            return $"to be equal to {expected}";
        }

        return $"to be equal to {expected} +-{_tolerance}";
    }

    protected override ValueTask<AssertionResult> GetResult(TimeOnly actualValue, TimeOnly expectedValue)
    {
        if (_tolerance is not null)
        {
            var min = expectedValue.Add(-_tolerance.Value);
            var max = expectedValue.Add(_tolerance.Value);

            return AssertionResult
                .FailIf(actualValue < min || actualValue > max,
                    $"the received value {actualValue} is outside the tolerances");
        }

        return AssertionResult
            .FailIf(actualValue != expected,
                $"the received value {actualValue} is different");
    }

    public void SetTolerance(TimeSpan tolerance)
    {
        _tolerance = tolerance;
    }
}

#endif
