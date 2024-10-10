using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWithMessage<TActual>(
    IDelegateSource<TActual> delegateSource,
    Func<Exception?, Exception?> exceptionSelector)
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; } = delegateSource.AssertionBuilder
        .AppendExpression("Message");

    public InvokableDelegateAssertionBuilder<TActual> EqualTo(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableDelegateAssertionBuilder<TActual> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return delegateSource.RegisterAssertion(new ThrowsWithMessageEqualToExpectedValueAssertCondition<TActual>(expected, stringComparison, exceptionSelector)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }

    public InvokableDelegateAssertionBuilder<TActual> Containing(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Containing(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableDelegateAssertionBuilder<TActual> Containing(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return delegateSource.RegisterAssertion(new ThrowsWithMessageContainingExpectedValueAssertCondition<TActual>(expected, stringComparison, exceptionSelector)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
}