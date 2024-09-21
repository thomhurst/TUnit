using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWithMessage<TActual>
{
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public ExceptionWithMessage(AssertionBuilder<TActual> assertionBuilder, Func<Exception?, Exception?> exceptionSelector)
    {
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = assertionBuilder
            .AppendExpression("Message");
    }

    public InvokableDelegateAssertionBuilder<TActual> EqualTo(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableDelegateAssertionBuilder<TActual> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return (InvokableDelegateAssertionBuilder<TActual>)new ThrowsWithMessageEqualToAssertCondition<TActual>(expected, stringComparison, _exceptionSelector)
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }

    public InvokableDelegateAssertionBuilder<TActual> Containing(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Containing(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableDelegateAssertionBuilder<TActual> Containing(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return (InvokableDelegateAssertionBuilder<TActual>)new ThrowsWithMessageContainingAssertCondition<TActual>(expected, stringComparison, _exceptionSelector)
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
}