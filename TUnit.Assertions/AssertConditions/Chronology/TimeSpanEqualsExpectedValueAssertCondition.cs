namespace TUnit.Assertions.AssertConditions.Chronology;

public class TimeSpanEqualsExpectedValueAssertCondition(TimeSpan expected) : ExpectedValueAssertCondition<TimeSpan, TimeSpan>(expected) 
{
    private TimeSpan? _tolerance;
    
    protected override string GetFailureMessage(TimeSpan actualValue, TimeSpan expectedValue) => $"""
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

    protected override bool Passes(TimeSpan actualValue, TimeSpan expectedValue)
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