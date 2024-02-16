using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageEqualToAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, string, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    private readonly StringComparison _stringComparison;

    public ThrowsWithMessageEqualToAssertCondition(AssertionBuilder<TActual> assertionBuilder, string expected, StringComparison stringComparison) : base(assertionBuilder, expected)
    {
        _stringComparison = stringComparison;
    }
    
    protected override string DefaultMessage => $"Message was {Exception?.Message} instead of {ExpectedValue}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (exception is null)
        {
            WithMessage((_, _) => "No exception was thrown");
            return false;
        }
        
        return string.Equals(exception.Message, ExpectedValue, _stringComparison);
    }
}