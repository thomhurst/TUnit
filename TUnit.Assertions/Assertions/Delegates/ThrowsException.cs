using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Delegates;

public class ThrowsException<TActual, TException> where TException : Exception
{
    private readonly IDelegateSource<TActual> _delegateSource;
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    private InvokableDelegateAssertionBuilder<TActual> _delegateAssertionBuilder;
    private IDelegateSource<TActual> delegateSource;
    private Func<Exception?, Exception?> exceptionSelector;

    public ThrowsException(InvokableDelegateAssertionBuilder<TActual> delegateAssertionBuilder, IDelegateSource<TActual> delegateSource, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "")
    {
        _delegateAssertionBuilder = delegateAssertionBuilder;
        _delegateSource = delegateSource;
        _exceptionSelector = exceptionSelector;
    }

    public ThrowsException<TActual, TException> WithMessageMatching(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        _delegateSource.RegisterAssertion(new ThrowsWithMessageMatchingAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, _exceptionSelector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, TException> WithMessage(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        _delegateSource.RegisterAssertion(new ThrowsWithMessageAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, _exceptionSelector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public TaskAwaiter<TException?> GetAwaiter() => _delegateAssertionBuilder.GetAwaiterWithException<TException>();
}