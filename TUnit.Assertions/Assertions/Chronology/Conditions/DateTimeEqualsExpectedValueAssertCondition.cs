﻿namespace TUnit.Assertions.AssertConditions.Chronology;

public class DateTimeEqualsExpectedValueAssertCondition(DateTime expected) : ExpectedValueAssertCondition<DateTime, DateTime>(expected) 
{
    private TimeSpan? _tolerance;

    protected override string GetExpectation()
    {
        if (_tolerance == null || _tolerance == default)
        {
            return $"to be equal to {expected}";
        }

        return $"to be equal to {expected} +-{_tolerance}";
    }

    protected override AssertionResult GetResult(DateTime actualValue, DateTime expectedValue)
    {
        if (_tolerance is not null)
        {
            var min = expectedValue - _tolerance;
            var max = expectedValue + _tolerance;

            return AssertionResult
                .FailIf(
                    () => actualValue < min || actualValue > max,
                    () => $"the received value {actualValue} is outside the tolerances");
        }

        return AssertionResult
            .FailIf(
                () => actualValue != expected,
                () => $"the received value {actualValue} is different");
    }

    public void SetTolerance(TimeSpan tolerance)
    {
        _tolerance = tolerance;
    }
}