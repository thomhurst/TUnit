namespace TUnit.Assertions.AssertConditions.Chronology;

public class DateOnlyEqualsExpectedValueAssertCondition(DateOnly expected) : ExpectedValueAssertCondition<DateOnly, DateOnly>(expected) 
{
    private int? _tolerance;
    
    protected override string GetFailureMessage(DateOnly actualValue, DateOnly expectedValue) => $"""
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

    protected override bool Passes(DateOnly actualValue, DateOnly expectedValue)
    {
        if (_tolerance is not null)
        {
            var min = expectedValue.AddDays(-_tolerance.Value);
            var max = expectedValue.AddDays(_tolerance.Value);
            
            return actualValue >= min && actualValue <= max;
        }
        
        return actualValue == expectedValue;
    }

    public void SetTolerance(int toleranceDays)
    {
        _tolerance = toleranceDays;
    }
}