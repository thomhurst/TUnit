namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageEqualToAssertCondition<TActual> : AssertCondition<TActual, string>
{
    private readonly StringComparison _stringComparison;
    private readonly Func<Exception?, Exception?> _exceptionSelector;

    public ThrowsWithMessageEqualToAssertCondition(string expected, StringComparison stringComparison, Func<Exception?, Exception?> exceptionSelector) : base(expected)
    {
        _stringComparison = stringComparison;
        _exceptionSelector = exceptionSelector;
    }
    
    protected internal override string GetFailureMessage() => $"Message was {_exceptionSelector(Exception)?.Message} instead of {ExpectedValue}";

    private protected override bool Passes(TActual? actualValue, Exception? rootException)
    {
        var exception = _exceptionSelector(rootException);

        if (exception is null)
        {
            OverriddenMessage = "Exception is null";
            return false;
        }
        
        return string.Equals(exception.Message, ExpectedValue, _stringComparison);
    }
}