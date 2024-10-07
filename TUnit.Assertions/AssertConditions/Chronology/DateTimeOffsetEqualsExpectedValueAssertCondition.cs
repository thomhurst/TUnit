﻿namespace TUnit.Assertions.AssertConditions.Chronology;

public class DateTimeOffsetEqualsExpectedValueAssertCondition(DateTimeOffset expected) : ExpectedValueAssertCondition<DateTimeOffset, DateTimeOffset>(expected) 
{
    private TimeSpan? _tolerance;
    
    protected override string GetFailureMessage(DateTimeOffset actualValue, DateTimeOffset expectedValue) => $"""
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

    protected override bool Passes(DateTimeOffset actualValue, DateTimeOffset expectedValue)
    {
        if (_tolerance is not null)
        {
            var min = expectedValue - _tolerance;
            var max = expectedValue + _tolerance;
            
            return actualValue >= min && actualValue <= max;
        }
        
        return actualValue == expectedValue;
    }

    public void SetTolerance(TimeSpan tolerance)
    {
        _tolerance = tolerance;
    }
}