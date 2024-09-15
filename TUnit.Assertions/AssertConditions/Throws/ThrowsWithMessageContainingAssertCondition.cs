using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageContainingAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, string, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;
    private readonly Func<Exception?, Exception?> _exceptionSelector;

    public ThrowsWithMessageContainingAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, string expected,
        StringComparison stringComparison, Func<Exception?, Exception?> exceptionSelector) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
        _exceptionSelector = exceptionSelector;
    }
    
    protected override string DefaultMessage => $"Message '{_exceptionSelector(Exception)?.Message}' did not contain '{ExpectedValue}'";

    protected internal override bool Passes(TActual? actualValue, Exception? rootException)
    {
        var exception = _exceptionSelector(rootException);
        
        if (exception is null)
        {
            WithMessage((_, _) => "Exception is null");
            return false;
        }
        
        if (ExpectedValue is null)
        {
            WithMessage((_, _) => "Expected message is null");
            return false;
        }
        
        return exception.Message.Contains(ExpectedValue, _stringComparison);
    }
}