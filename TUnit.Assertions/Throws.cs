using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions;

public class Throws<TActual> : Connector<TActual>
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; }

    public Throws(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public WithMessage<TActual> WithMessage => new(AssertionBuilder, ConnectorType, OtherAssertCondition);

    public BaseAssertCondition<TActual> Nothing => Wrap(new ThrowsNothingAssertCondition<TActual>(AssertionBuilder));

    public BaseAssertCondition<TActual> Exception =>
        Wrap(new ThrowsAnythingAssertCondition<TActual>(AssertionBuilder));

    public BaseAssertCondition<TActual> TypeOf<TExpected>() => Wrap(new ThrowsExactTypeOfAssertCondition<TActual, TExpected>(AssertionBuilder));

    public BaseAssertCondition<TActual> SubClassOf<TExpected>() =>
        Wrap(new ThrowsSubClassOfAssertCondition<TActual, TExpected>(AssertionBuilder));
}