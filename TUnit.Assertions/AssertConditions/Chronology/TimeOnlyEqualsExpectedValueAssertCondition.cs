namespace TUnit.Assertions.AssertConditions.Chronology;

public class TimeOnlyEqualsExpectedValueAssertCondition(TimeOnly expected) : ExpectedValueAssertCondition<TimeOnly, TimeOnly>(expected) 
{
    private TimeSpan? _tolerance;
    
    protected override string GetFailureMessage(TimeOnly actualValue, TimeOnly expectedValue) => $"""
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

    protected override bool Passes(TimeOnly actualValue, TimeOnly expectedValue)
    {
        if (_tolerance is not null)
        {
            var min = expectedValue.Add(-_tolerance.Value);
            var max = expectedValue.Add(_tolerance.Value);
            
            return actualValue >= min && actualValue <= max;
        }
        
        return actualValue == expectedValue;
    }

    public void SetTolerance(TimeSpan tolerance)
    {
        _tolerance = tolerance;
    }
}