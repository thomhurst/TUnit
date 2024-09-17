using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }
    
    public ThrowsException(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "")
    {
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = assertionBuilder
            .AppendExpression(callerMemberName);
    }

    public ExceptionWith<TActual, TAnd, TOr> With => new(AssertionBuilder, _exceptionSelector);

    public InvokableAssertionBuilder<TActual, TAnd, TOr> OfAnyType() =>
        new ThrowsAnythingAssertCondition<TActual>()
            .ChainedTo(AssertionBuilder, []);

    public InvokableAssertionBuilder<TActual, TAnd, TOr> OfType<TExpected>() => new ThrowsExactTypeOfAssertCondition<TActual, TExpected>()
        .ChainedTo(AssertionBuilder, [typeof(TExpected).Name]);

    public InvokableAssertionBuilder<TActual, TAnd, TOr> SubClassOf<TExpected>() =>
        new ThrowsSubClassOfAssertCondition<TActual, TExpected>()
            .ChainedTo(AssertionBuilder, [typeof(TExpected).Name]);
    
    public InvokableAssertionBuilder<TActual, TAnd, TOr> WithCustomCondition(Func<Exception?, bool> action, Func<Exception?, string> messageFactory, [CallerArgumentExpression("action")] string expectedExpression = "") =>
        new DelegateAssertCondition<TActual,Exception>(default,
            (_, exception, _, _) => action(_exceptionSelector(exception)),
            (_, exception, _) => messageFactory(_exceptionSelector(exception))
        ).ChainedTo(AssertionBuilder, [expectedExpression]);

    public TaskAwaiter GetAwaiter() => OfAnyType().GetAwaiter();
}