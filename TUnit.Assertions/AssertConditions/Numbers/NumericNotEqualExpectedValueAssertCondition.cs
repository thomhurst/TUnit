using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Numbers;

public class NumericNotEqualExpectedValueAssertCondition<TActual>(TActual expected) : ExpectedValueAssertCondition<TActual, TActual>(expected) 
    where TActual : INumber<TActual>
{
    private TActual? _tolerance;
    
    protected override string GetFailureMessage(TActual? actualValue, TActual? expectedValue) => $"""
                                                 Expected Not Equal To: {ExpectedValue}{WithToleranceMessage()}
                                                 """;

    private string WithToleranceMessage()
    {
        if (_tolerance == null)
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
        
        if (_tolerance != null)
        {
            var min = expectedValue - _tolerance;
            var max = expectedValue + _tolerance;
            
            return actualValue < min || actualValue > max;
        }
        
        return actualValue != expectedValue;
    }

    public void SetTolerance(TActual tolerance)
    {
        _tolerance = tolerance;
    }
}