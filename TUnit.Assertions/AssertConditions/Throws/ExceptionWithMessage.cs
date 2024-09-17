using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWithMessage<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }
    
    public ExceptionWithMessage(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, Func<Exception?, Exception?> exceptionSelector)
    {
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = assertionBuilder
            .AppendExpression("Message");
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> EqualTo(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return new ThrowsWithMessageEqualToAssertCondition<TActual, TAnd, TOr>(expected, stringComparison, _exceptionSelector)
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> Containing(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Containing(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> Containing(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return new ThrowsWithMessageContainingAssertCondition<TActual, TAnd, TOr>(expected, stringComparison, _exceptionSelector)
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
}