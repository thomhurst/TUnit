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
    
    protected override string DefaultMessage => $"Message was {_exceptionSelector(Exception)?.Message} instead of {ExpectedValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? rootException, string? rawValueExpression)
    {
        var exception = _exceptionSelector(rootException);

        if (exception is null)
        {
            WithMessage((_, _, _) => "Exception is null");
            return false;
        }
        
        return string.Equals(exception.Message, ExpectedValue, _stringComparison);
    }
}