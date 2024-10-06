using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWithMessage<TActual>
{
    private readonly IDelegateSource<TActual> _delegateSource;
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public ExceptionWithMessage(IDelegateSource<TActual> delegateSource, Func<Exception?, Exception?> exceptionSelector)
    {
        _delegateSource = delegateSource;
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = delegateSource.AssertionBuilder
            .AppendExpression("Message");
    }

    public InvokableDelegateAssertionBuilder<TActual> EqualTo(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableDelegateAssertionBuilder<TActual> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return _delegateSource.RegisterAssertion(new ThrowsWithMessageEqualToExpectedValueAssertCondition<TActual>(expected, stringComparison, _exceptionSelector)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }

    public InvokableDelegateAssertionBuilder<TActual> Containing(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Containing(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableDelegateAssertionBuilder<TActual> Containing(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return _delegateSource.RegisterAssertion(new ThrowsWithMessageContainingExpectedValueAssertCondition<TActual>(expected, stringComparison, _exceptionSelector)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
}