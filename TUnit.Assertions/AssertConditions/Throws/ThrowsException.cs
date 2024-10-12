using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual>(
    IDelegateSource<TActual> delegateSource,
    Func<Exception?, Exception?> exceptionSelector,
    [CallerMemberName] string callerMemberName = "")
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; } = delegateSource.AssertionBuilder
        .AppendExpression(callerMemberName);

    public ExceptionWith<TActual> With => new(delegateSource, exceptionSelector);

    public InvokableDelegateAssertionBuilder<TActual> OfAnyType() =>
        delegateSource.RegisterAssertion(new ThrowsAnythingExpectedValueAssertCondition<TActual>()
            , []);

    public InvokableDelegateAssertionBuilder<TActual> OfType<TExpected>() where TExpected : Exception => delegateSource.RegisterAssertion(new ThrowsExactTypeOfDelegateAssertCondition<TActual, TExpected>()
        , [typeof(TExpected).Name]);

    public InvokableDelegateAssertionBuilder<TActual> SubClassOf<TExpected>() =>
        delegateSource.RegisterAssertion(new ThrowsSubClassOfExpectedValueAssertCondition<TActual, TExpected>()
            , [typeof(TExpected).Name]);
    
    public InvokableDelegateAssertionBuilder<TActual> WithCustomCondition(Func<Exception?, bool> action, Func<Exception?, string> messageFactory, [CallerArgumentExpression("action")] string expectedExpression = "") =>
        delegateSource.RegisterAssertion(new FuncValueAssertCondition<TActual,Exception>(default,
            (_, exception, _) => action(exceptionSelector(exception)),
            (_, exception, _) => messageFactory(exceptionSelector(exception))
        ), [expectedExpression]);

    public TaskAwaiter GetAwaiter()
    {
        Task task = OfAnyType().ProcessAssertionsAsync();
        return task.GetAwaiter();
    }
}