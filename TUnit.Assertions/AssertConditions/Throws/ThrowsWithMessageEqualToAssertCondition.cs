namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageEqualToAssertCondition<TActual> : AssertCondition<TActual, string>
{
    private readonly StringComparison _stringComparison;

    public ThrowsWithMessageEqualToAssertCondition(AssertionBuilder<TActual> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected override string DefaultMessage => $"Message was {Exception?.Message} instead of {ExpectedValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        
        return string.Equals(exception.Message, ExpectedValue, _stringComparison);
    }
}