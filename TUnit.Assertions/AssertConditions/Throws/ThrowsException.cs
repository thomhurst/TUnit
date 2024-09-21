using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual>
{
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public ThrowsException(AssertionBuilder<TActual> assertionBuilder, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "")
    {
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = assertionBuilder
            .AppendExpression(callerMemberName);
    }

    public ExceptionWith<TActual> With => new(AssertionBuilder, _exceptionSelector);

    public InvokableDelegateAssertionBuilder<TActual> OfAnyType() =>
        (InvokableDelegateAssertionBuilder<TActual>)new ThrowsAnythingAssertCondition<TActual>()
            .ChainedTo(AssertionBuilder, []);

    public InvokableDelegateAssertionBuilder<TActual> OfType<TExpected>() => (InvokableDelegateAssertionBuilder<TActual>)new ThrowsExactTypeOfAssertCondition<TActual, TExpected>()
        .ChainedTo(AssertionBuilder, [typeof(TExpected).Name]);

    public InvokableDelegateAssertionBuilder<TActual> SubClassOf<TExpected>() =>
        (InvokableDelegateAssertionBuilder<TActual>)new ThrowsSubClassOfAssertCondition<TActual, TExpected>()
            .ChainedTo(AssertionBuilder, [typeof(TExpected).Name]);
    
    public InvokableDelegateAssertionBuilder<TActual> WithCustomCondition(Func<Exception?, bool> action, Func<Exception?, string> messageFactory, [CallerArgumentExpression("action")] string expectedExpression = "") =>
        (InvokableDelegateAssertionBuilder<TActual>)new DelegateAssertCondition<TActual,Exception>(default,
            (_, exception, _, _) => action(_exceptionSelector(exception)),
            (_, exception, _) => messageFactory(_exceptionSelector(exception))
        ).ChainedTo(AssertionBuilder, [expectedExpression]);

    public TaskAwaiter GetAwaiter() => OfAnyType().GetAwaiter();
}