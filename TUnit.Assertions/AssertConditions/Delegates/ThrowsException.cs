using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Delegates;

public class ThrowsException<TActual, TException>(
    InvokableDelegateAssertionBuilder<TActual> delegateAssertionBuilder,
    IDelegateSource<TActual> delegateSource,
    Func<Exception?, Exception?> exceptionSelector,
    [CallerMemberName] string callerMemberName = "")
    where TException : Exception
{
    private readonly IDelegateSource<TActual> _delegateSource = delegateSource;
    private readonly Func<Exception?, Exception?> _exceptionSelector = exceptionSelector;
    private IDelegateSource<TActual> delegateSource;
    private Func<Exception?, TException?> exceptionSelector;

    public ThrowsException<TActual, TException> WithMessageMatching(StringMatcher match, [CallerArgumentExpression("match")] string doNotPopulateThisValue = "")
    {
        _delegateSource.RegisterAssertion(new ThrowsWithMessageMatchingAssertCondition<TActual, TException>(match, _exceptionSelector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, TException> WithMessage(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        _delegateSource.RegisterAssertion(new ThrowsWithMessageAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, _exceptionSelector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, Exception> WithInnerException()
    {
        _delegateSource.AssertionBuilder.AppendExpression($"{nameof(WithInnerException)}()");
        return new(delegateAssertionBuilder, _delegateSource, e => _exceptionSelector(e)?.InnerException);
    }

    public TaskAwaiter<TException?> GetAwaiter()
    {
        var task = delegateAssertionBuilder.ProcessAssertionsAsync(
            d => d.Exception as TException);
        return task.GetAwaiter();
    }
}