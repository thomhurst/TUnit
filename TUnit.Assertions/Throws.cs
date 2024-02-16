using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions;

public class Throws<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; }

    public Throws(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder
            .AppendConnector(connectorType)
            .AppendExpression("Throws");
    }

    public ExceptionWith<TActual, TAnd, TOr> With => new(AssertionBuilder, ConnectorType, OtherAssertCondition, exception => exception);

    public BaseAssertCondition<TActual, TAnd, TOr> Nothing() => Wrap(new ThrowsNothingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(string.Empty)));

    public BaseAssertCondition<TActual, TAnd, TOr> Exception() =>
        Wrap(new ThrowsAnythingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder));

    public BaseAssertCondition<TActual, TAnd, TOr> TypeOf<TExpected>() => Wrap(new ThrowsExactTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName)));

    public BaseAssertCondition<TActual, TAnd, TOr> SubClassOf<TExpected>() =>
        Wrap(new ThrowsSubClassOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName)));
    
    public BaseAssertCondition<TActual, TAnd, TOr> WithCustomCondition(Func<Exception?, bool> action, Func<Exception?, string> messageFactory, [CallerArgumentExpression("action")] string expectedExpression = "") =>
        Wrap(new DelegateAssertCondition<TActual,Exception,TAnd,TOr>(AssertionBuilder.AppendCallerMethod(expectedExpression),
            default,
            (_, exception, _) => action(exception),
            (_, exception) => messageFactory(exception)
        ));
}