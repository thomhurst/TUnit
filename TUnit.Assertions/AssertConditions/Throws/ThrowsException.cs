using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual, TException>(
    InvokableDelegateAssertionBuilder<TActual> delegateAssertionBuilder,
    IDelegateSource<TActual> delegateSource,
    Func<Exception?, Exception?> exceptionSelector)
    where TException : Exception
{
    public ThrowsException<TActual, TException> WithMessageMatching(StringMatcher match, [CallerArgumentExpression("match")] string doNotPopulateThisValue = "")
    {
        delegateSource.RegisterAssertion(
            new ThrowsWithMessageMatchingAssertCondition<TActual, TException>(match, exceptionSelector),
            [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, TException> WithMessage(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        delegateSource.RegisterAssertion(
            new ThrowsWithMessageAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, exceptionSelector),
            [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, Exception> WithInnerException()
    {
        delegateSource.AssertionBuilder.AppendExpression($"{nameof(WithInnerException)}()");
        return new(delegateAssertionBuilder, delegateSource, e => exceptionSelector(e)?.InnerException);
    }

    public TaskAwaiter<TException> GetAwaiter()
    {
        var task = delegateAssertionBuilder.ProcessAssertionsAsync(
            d => d.Exception as TException);
        return task.GetAwaiter()!;
    }
}