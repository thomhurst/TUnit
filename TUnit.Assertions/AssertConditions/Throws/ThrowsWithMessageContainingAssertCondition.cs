namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageContainingAssertCondition<TActual> : AssertCondition<TActual, string>
{
    private readonly StringComparison _stringComparison;
    private readonly Func<Exception?, Exception?> _exceptionSelector;

    public ThrowsWithMessageContainingAssertCondition(string expected,
        StringComparison stringComparison, Func<Exception?, Exception?> exceptionSelector) : base(expected)
    {
        _stringComparison = stringComparison;
        _exceptionSelector = exceptionSelector;
    }
    
    protected internal override string GetFailureMessage() => $"Message '{_exceptionSelector(Exception)?.Message}' did not contain '{ExpectedValue}'";

    protected override bool Passes(TActual? actualValue, Exception? rootException)
    {
        var exception = _exceptionSelector(rootException);
        
        if (exception is null)
        {
            OverriddenMessage = "Exception is null";
            return false;
        }
        
        if (ExpectedValue is null)
        {
            OverriddenMessage = "Expected message is null";
            return false;
        }
        
        return exception.Message.Contains(ExpectedValue, _stringComparison);
    }
}