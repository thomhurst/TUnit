using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class NumericEqualsExpectedValueAssertCondition<TActual>(TActual expected) : ExpectedValueAssertCondition<TActual, TActual>(expected) 
    where TActual : INumber<TActual>
{
    private TActual? _tolerance;
    private bool _isToleranceSet;

    protected override string GetExpectation()
    {
        if (!_isToleranceSet)
        {
            return $"to be equal to {expected}";
        }

        return $"to be equal to {expected} +-{_tolerance}";
    }

    protected override AssertionResult GetResult(TActual? actualValue, TActual? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return AssertionResult.Passed;
        }

        if (actualValue is null || expectedValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => actualValue is null,
                    "it is null")
                .OrFailIf(
                    () => expectedValue is null,
                    "it is not null");
        }

        if (_isToleranceSet && _tolerance is not null)
        {
            var min = expectedValue - _tolerance;
            var max = expectedValue + _tolerance;

            return AssertionResult
                .FailIf(
                    () => actualValue < min || actualValue > max,
                    $"the received value {actualValue} is outside the tolerances");
        }

        return AssertionResult
            .FailIf(
                () => actualValue != expected,
                $"the received value {actualValue} is different");
    }

    public void SetTolerance(TActual tolerance)
    {
        _tolerance = tolerance;
        _isToleranceSet = true;
    }
}