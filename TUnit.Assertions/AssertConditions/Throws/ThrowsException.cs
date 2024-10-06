using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual>
{
    private readonly IDelegateSource<TActual> _delegateSource;
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public ThrowsException(IDelegateSource<TActual> delegateSource, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "")
    {
        _delegateSource = delegateSource;
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = delegateSource.AssertionBuilder
            .AppendExpression(callerMemberName);
    }

    public ExceptionWith<TActual> With => new(_delegateSource, _exceptionSelector);

    public InvokableDelegateAssertionBuilder<TActual> OfAnyType() =>
        _delegateSource.RegisterAssertion(new ThrowsAnythingExpectedValueAssertCondition<TActual>()
            , []);

    public InvokableDelegateAssertionBuilder<TActual> OfType<TExpected>() where TExpected : Exception => _delegateSource.RegisterAssertion(new ThrowsExactTypeOfDelegateAssertCondition<TActual, TExpected>()
        , [typeof(TExpected).Name]);

    public InvokableDelegateAssertionBuilder<TActual> SubClassOf<TExpected>() =>
        _delegateSource.RegisterAssertion(new ThrowsSubClassOfExpectedValueAssertCondition<TActual, TExpected>()
            , [typeof(TExpected).Name]);
    
    public InvokableDelegateAssertionBuilder<TActual> WithCustomCondition(Func<Exception?, bool> action, Func<Exception?, string> messageFactory, [CallerArgumentExpression("action")] string expectedExpression = "") =>
        _delegateSource.RegisterAssertion(new FuncValueAssertCondition<TActual,Exception>(default,
            (_, exception, _) => action(_exceptionSelector(exception)),
            (_, exception, _) => messageFactory(_exceptionSelector(exception))
        ), [expectedExpression]);

    public TaskAwaiter GetAwaiter() => OfAnyType().GetAwaiter();
}