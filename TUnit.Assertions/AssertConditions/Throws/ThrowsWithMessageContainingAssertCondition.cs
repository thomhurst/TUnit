namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageContainingAssertCondition<TActual> : AssertCondition<TActual, string>
{
    private readonly StringComparison _stringComparison;

    public ThrowsWithMessageContainingAssertCondition(AssertionBuilder<TActual> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected override string DefaultMessage => $"Message '{Exception?.Message}' did not contain '{ExpectedValue}'";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(ExpectedValue);
        
        return exception.Message.Contains(ExpectedValue, _stringComparison);
    }
}