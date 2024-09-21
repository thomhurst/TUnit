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

    public ExceptionWith<TActual> With => new(AssertionBuilder, _exceptionSelector);

    public InvokableDelegateAssertionBuilder<TActual> OfAnyType() =>
        _delegateSource.RegisterAssertion(new ThrowsAnythingAssertCondition<TActual>()
            , []);

    public InvokableDelegateAssertionBuilder<TActual> OfType<TExpected>() => _delegateSource.RegisterAssertion(new ThrowsExactTypeOfAssertCondition<TActual, TExpected>()
        , [typeof(TExpected).Name]);

    public InvokableDelegateAssertionBuilder<TActual> SubClassOf<TExpected>() =>
        _delegateSource.RegisterAssertion(new ThrowsSubClassOfAssertCondition<TActual, TExpected>()
            , [typeof(TExpected).Name]);
    
    public InvokableDelegateAssertionBuilder<TActual> WithCustomCondition(Func<Exception?, bool> action, Func<Exception?, string> messageFactory, [CallerArgumentExpression("action")] string expectedExpression = "") =>
        _delegateSource.RegisterAssertion(new DelegateAssertCondition<TActual,Exception>(default,
            (_, exception, _, _) => action(_exceptionSelector(exception)),
            (_, exception, _) => messageFactory(_exceptionSelector(exception))
        ), [expectedExpression]);

    public TaskAwaiter GetAwaiter() => OfAnyType().GetAwaiter();
}