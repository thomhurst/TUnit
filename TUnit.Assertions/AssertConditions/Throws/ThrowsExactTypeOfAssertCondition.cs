using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsExactTypeOfAssertCondition<TActual, TExpected, TAnd, TOr> : AssertCondition<TActual, TExpected, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    public ThrowsExactTypeOfAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder,
        Func<Exception?, Exception?> exceptionSelector) : base(assertionBuilder, default)
    {
    }
    
    protected override string DefaultMessage => $"A {Exception?.GetType().Name} was thrown instead of {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (exception is null)
        {
            WithMessage((_, _) => "Exception is null");
            return false;
        }
        
        return exception.GetType() == typeof(TExpected);
    }
}