using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual, TException> where TException : Exception
{
    private readonly InvokableDelegateAssertionBuilder<TActual> _delegateAssertionBuilder;
    private readonly IDelegateSource<TActual> _source;
    private readonly Func<Exception?, Exception?> _selector;

    public ThrowsException(InvokableDelegateAssertionBuilder<TActual> delegateAssertionBuilder,
        IDelegateSource<TActual> source,
        Func<Exception?, Exception?> selector)
    {
        _delegateAssertionBuilder = delegateAssertionBuilder;
        _source = source;
        _selector = selector;
    }

    public ThrowsException<TActual, TException> WithMessageMatching(StringMatcher match, [CallerArgumentExpression(nameof(match))] string doNotPopulateThisValue = "")
    {
        _source.RegisterAssertion(new ThrowsWithMessageMatchingAssertCondition<TActual, TException>(match, _selector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, TException> WithMessage(string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        _source.RegisterAssertion(new ThrowsWithMessageAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, _selector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, Exception> WithInnerException()
    {
        _source.AssertionBuilder.AppendExpression($"{nameof(WithInnerException)}()");
        return new(_delegateAssertionBuilder, _source, e => _selector(e)?.InnerException);
    }

    public TaskAwaiter<TException?> GetAwaiter()
    {
        var task = _delegateAssertionBuilder.ProcessAssertionsAsync(
            d => d.Exception as TException);
        return task.GetAwaiter();
    }
    
    public AndConvertedTypeAssertionBuilder<TActual, TException> And => new(_delegateAssertionBuilder, async () =>
    {
        var value = await this;
        return new AssertionData<TException>(value, null, _delegateAssertionBuilder.ActualExpression);

    }, _delegateAssertionBuilder.ActualExpression!, _delegateAssertionBuilder.ExpressionBuilder!.Append(".And"));
    
    public DelegateOr<TActual> Or => _delegateAssertionBuilder.Or;
}