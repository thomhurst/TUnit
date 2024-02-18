using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;

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
        Wrap(new ThrowsAnythingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(null), _exceptionSelector));

    public BaseAssertCondition<TActual, TAnd, TOr> OfType<TExpected>() => Wrap(new ThrowsExactTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName), _exceptionSelector));

    public BaseAssertCondition<TActual, TAnd, TOr> SubClassOf<TExpected>() =>
        Wrap(new ThrowsSubClassOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName), _exceptionSelector));
    
    public BaseAssertCondition<TActual, TAnd, TOr> WithCustomCondition(Func<Exception?, bool> action, Func<Exception?, string> messageFactory, [CallerArgumentExpression("action")] string doNotPopulateThisValue = "") =>
        Wrap(new DelegateAssertCondition<TActual,Exception,TAnd,TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            default,
            (_, exception, _, self) => action(_exceptionSelector(exception)),
            (_, exception) => messageFactory(_exceptionSelector(exception))
        ));
}