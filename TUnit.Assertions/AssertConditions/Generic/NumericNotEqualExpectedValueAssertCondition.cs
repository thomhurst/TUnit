using System.Numerics;

namespace TUnit.Assertions.AssertConditions.Generic;

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

    protected override bool Passes(TActual? actualValue, TActual? expectedValue)
    {
        if (actualValue == null && ExpectedValue == null)
        {
            return true;
        }

        if (actualValue == null || ExpectedValue == null)
        {
            return false;
        }
        
        if (_tolerance != null)
        {
            var min = ExpectedValue - _tolerance;
            var max = ExpectedValue + _tolerance;
            
            return actualValue < min || actualValue > max;
        }
        
        return actualValue != ExpectedValue;
    }

    public void SetTolerance(TActual tolerance)
    {
        _tolerance = tolerance;
    }
}