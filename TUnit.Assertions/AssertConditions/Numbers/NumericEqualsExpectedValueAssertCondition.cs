using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class NumericEqualsExpectedValueAssertCondition<TActual>(TActual expected) : ExpectedValueAssertCondition<TActual, TActual>(expected) 
    where TActual : INumber<TActual>
{
    private TActual? _tolerance;
    
    protected override string GetFailureMessage(TActual? actualValue, TActual? expectedValue) => $"""
                                                 Expected: {ExpectedValue}{WithToleranceMessage()}
                                                 Received: {ActualValue}
                                                 """;

    private string WithToleranceMessage()
    {
        if (_tolerance == null || _tolerance == default)
        {
            return string.Empty;
        }

        return $" +-{_tolerance}";
    }

    protected override AssertionResult Passes(TActual? actualValue, TActual? expectedValue)
    {
        if (actualValue == null && expectedValue == null)
        {
            return true;
        }

        if (actualValue == null || expectedValue == null)
        {
            return false;
        }
        
        if (_tolerance != default && _tolerance != null)
        {
            var min = expectedValue - _tolerance;
            var max = expectedValue + _tolerance;
            
            return actualValue >= min && actualValue <= max;
        }
        
        return actualValue == expectedValue;
    }

    public void SetTolerance(TActual tolerance)
    {
        _tolerance = tolerance;
    }
}