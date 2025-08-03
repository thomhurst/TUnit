using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual, TException> where TException : Exception
{
    private readonly InvokableDelegateAssertionBuilder _delegateAssertionBuilder;
    private readonly IDelegateSource _source;
    private readonly Func<Exception?, Exception?> _selector;

    public ThrowsException(InvokableDelegateAssertionBuilder delegateAssertionBuilder,
        IDelegateSource source,
        Func<Exception?, Exception?> selector)
    {
        _delegateAssertionBuilder = delegateAssertionBuilder;
        _source = source;
        _selector = selector;
    }

    public ThrowsException<TActual, TException> WithMessageMatching(StringMatcher match, [CallerArgumentExpression(nameof(match))] string? doNotPopulateThisValue = null)
    {
        _source.RegisterAssertion(new ThrowsWithMessageMatchingAssertCondition<TActual, TException>(match, _selector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, TException> WithMessage(string expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
    {
        _source.RegisterAssertion(new ThrowsWithMessageAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, _selector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, TException> WithMessageContaining(string expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
    {
        _source.RegisterAssertion(new ThrowsWithMessageContainingAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, _selector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, TException> WithMessageContaining(string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null, [CallerArgumentExpression(nameof(stringComparison))] string? doNotPopulateThisValue2 = null)
    {
        _source.RegisterAssertion(new ThrowsWithMessageContainingAssertCondition<TActual, TException>(expected, stringComparison, _selector)
            , [doNotPopulateThisValue, doNotPopulateThisValue2]);
        return this;
    }

    public ThrowsException<TActual, Exception> WithInnerException()
    {
        _source.AppendExpression($"{nameof(WithInnerException)}()");
        return new ThrowsException<TActual, Exception>(_delegateAssertionBuilder, _source, e => _selector(e)?.InnerException);
    }

    public TaskAwaiter<TException?> GetAwaiter()
    {
        var task = _delegateAssertionBuilder.ProcessAssertionsAsync(d => Task.FromResult(d.Exception as TException));
        return task.GetAwaiter();
    }

    public ValueAnd<TException> And =>
        new(
            new AndAssertionBuilder(
                _delegateAssertionBuilder.RegisterConversionAssertion<TException>()
                    .AppendConnector(ChainType.And)
            )
        );

    public DelegateOr<object?> Or => _delegateAssertionBuilder.Or;

    internal void RegisterAssertion(Func<Func<Exception?, Exception?>, BaseAssertCondition<TActual>> conditionFunc, string?[] argumentExpressions, [CallerMemberName] string? caller = null) => _source.RegisterAssertion(conditionFunc(_selector), argumentExpressions, caller);

    public static explicit operator Task<TException?>(ThrowsException<TActual, TException> throwsException) => Task.Run(async () => await throwsException);
}
