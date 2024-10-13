using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual, TException>(
    InvokableDelegateAssertionBuilder<TActual> delegateAssertionBuilder,
    IDelegateSource<TActual> source,
    Func<Exception?, Exception?> selector,
    [CallerMemberName] string callerMemberName = "")
    where TException : Exception
{
    public ThrowsException<TActual, TException> WithMessageMatching(StringMatcher match, [CallerArgumentExpression("match")] string doNotPopulateThisValue = "")
    {
        source.RegisterAssertion(new ThrowsWithMessageMatchingAssertCondition<TActual, TException>(match, selector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, TException> WithMessage(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        source.RegisterAssertion(new ThrowsWithMessageAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, selector)
            , [doNotPopulateThisValue]);
        return this;
    }

    public ThrowsException<TActual, Exception> WithInnerException()
    {
        source.AssertionBuilder.AppendExpression($"{nameof(WithInnerException)}()");
        return new(delegateAssertionBuilder, source, e => selector(e)?.InnerException);
    }

    public TaskAwaiter<TException?> GetAwaiter()
    {
        var task = delegateAssertionBuilder.ProcessAssertionsAsync(
            d => d.Exception as TException);
        return task.GetAwaiter();
    }
    
    public AndConvertedTypeAssertionBuilder<TActual, TException> And => new(delegateAssertionBuilder, async () =>
    {
        var value = await this;
        return new AssertionData<TException>(value, null, delegateAssertionBuilder.ActualExpression);

    }, delegateAssertionBuilder.ActualExpression!, delegateAssertionBuilder.ExpressionBuilder!.Append(".And"));
    
    public DelegateOr<TActual> Or => delegateAssertionBuilder.Or;
}