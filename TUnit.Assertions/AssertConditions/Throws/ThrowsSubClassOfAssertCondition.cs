using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsSubClassOfAssertCondition<TActual, TExpected, TAnd, TOr> : AssertCondition<TActual, TExpected, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public ThrowsSubClassOfAssertCondition(AssertionBuilder<TActual> assertionBuilder,
        Func<Exception?, Exception?> exceptionSelector) : base(assertionBuilder, default)
    {
    }
    
    protected override string DefaultMessage => $"A {Exception?.GetType().Name} was thrown instead of subclass of {typeof(TExpected).Name}";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (exception is null)
        {
            WithMessage((_, _) => "Exception is null");
            return false;
        }        
        
        return exception.GetType().IsSubclassOf(typeof(TExpected));
    }
}