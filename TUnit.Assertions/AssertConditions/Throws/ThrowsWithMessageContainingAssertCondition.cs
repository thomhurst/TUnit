using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageContainingAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, string, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;

    public ThrowsWithMessageContainingAssertCondition(AssertionBuilder<TActual> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected override string DefaultMessage => $"Message '{Exception?.Message}' did not contain '{ExpectedValue}'";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
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