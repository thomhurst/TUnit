using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsException<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public ThrowsException(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "") : base(connectorType, otherAssertCondition)
    {
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = assertionBuilder
            .AppendExpression(callerMemberName);
    }

    public ExceptionWith<TActual, TAnd, TOr> With => new(AssertionBuilder, ConnectorType, OtherAssertCondition, _exceptionSelector);

    public BaseAssertCondition<TActual, TAnd, TOr> OfAnyType() =>
        Combine(new ThrowsAnythingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(null), _exceptionSelector));

    public BaseAssertCondition<TActual, TAnd, TOr> OfType<TExpected>() => Combine(new ThrowsExactTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName), _exceptionSelector));

    public BaseAssertCondition<TActual, TAnd, TOr> SubClassOf<TExpected>() =>
        Combine(new ThrowsSubClassOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName), _exceptionSelector));
    
    public BaseAssertCondition<TActual, TAnd, TOr> WithCustomCondition(Func<Exception?, bool> action, Func<Exception?, string> messageFactory, [CallerArgumentExpression("action")] string expectedExpression = "") =>
        Combine(new DelegateAssertCondition<TActual,Exception,TAnd,TOr>(AssertionBuilder.AppendCallerMethod(expectedExpression),
            default,
            (_, exception, _, _) => action(_exceptionSelector(exception)),
            (_, exception) => messageFactory(_exceptionSelector(exception))
        ));
}