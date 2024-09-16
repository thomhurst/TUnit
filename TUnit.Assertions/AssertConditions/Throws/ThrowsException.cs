using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual, TAnd, TOr>>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }
    
    public ThrowsException(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ChainType chainType, BaseAssertCondition<TActual>? otherAssertCondition, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "")
    {
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = assertionBuilder
            .AppendExpression(callerMemberName);
    }

    public ExceptionWith<TActual, TAnd, TOr> With => new(AssertionBuilder, _exceptionSelector);

    public BaseAssertCondition<TActual> OfAnyType() =>
        Combine(new ThrowsAnythingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(null), _exceptionSelector));

    public BaseAssertCondition<TActual> OfType<TExpected>() => Combine(new ThrowsExactTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName), _exceptionSelector));

    public BaseAssertCondition<TActual> SubClassOf<TExpected>() =>
        Combine(new ThrowsSubClassOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName), _exceptionSelector));
    
    public BaseAssertCondition<TActual> WithCustomCondition(Func<Exception?, bool> action, Func<Exception?, string> messageFactory, [CallerArgumentExpression("action")] string expectedExpression = "") =>
        Combine(new DelegateAssertCondition<TActual,Exception,TAnd,TOr>(AssertionBuilder.AppendCallerMethod(expectedExpression),
            default,
            (_, exception, _, _) => action(_exceptionSelector(exception)),
            (_, exception) => messageFactory(_exceptionSelector(exception))
        ));

    public TaskAwaiter GetAwaiter() => OfAnyType().GetAwaiter();
}