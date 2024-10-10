using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class NumericNotEqualExpectedValueAssertCondition<TActual>(TActual expected) : ExpectedValueAssertCondition<TActual, TActual>(expected) 
    where TActual : INumber<TActual>
{
    private TActual? _tolerance;

    protected override string GetExpectation()
    {
        if (_tolerance == null || _tolerance == default)
        {
            return $"to not be equal to {expected}";
        }

        return $"to not be equal to {expected} +-{_tolerance}";
    }

    protected override AssertionResult GetResult(TActual? actualValue, TActual? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is null,
                    "it is null");

        }
        
        if (_tolerance != null)
        {
            var min = expectedValue - _tolerance;
            var max = expectedValue + _tolerance;

            return AssertionResult
                .FailIf(
                    () => actualValue >= min && actualValue <= max,
                    $"found {actualValue}");
        }

        return AssertionResult.Passed;
    }

    public void SetTolerance(TActual tolerance)
    {
        _tolerance = tolerance;
    }
}