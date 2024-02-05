using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public class Is<TActual, TAnd, TOr> : Connector<TActual?, TAnd, TOr>
    where TAnd : And<TActual?, TAnd, TOr>, IAnd<TAnd, TActual?, TAnd, TOr>
    where TOr : Or<TActual?, TAnd, TOr>, IOr<TOr, TActual?, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual?> AssertionBuilder { get; }

    public Is(AssertionBuilder<TActual?> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<TActual?, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public BaseAssertCondition<TActual?, TAnd, TOr> EqualTo(TActual? expected)
    {
        return Wrap(new EqualsAssertCondition<TActual?, TAnd, TOr>(AssertionBuilder, expected));
    }

    public BaseAssertCondition<TActual?, TAnd, TOr> SameReference(TActual? expected)
    {
        return Wrap(new SameReferenceAssertCondition<TActual?, TActual?, TAnd, TOr>(AssertionBuilder, expected));
    }

    public BaseAssertCondition<TActual?, TAnd, TOr> Null() => Wrap(new NullAssertCondition<TActual?, TAnd, TOr>(AssertionBuilder));

    public BaseAssertCondition<TActual?, TAnd, TOr> TypeOf<TExpected>() => Wrap(new TypeOfAssertCondition<TActual?, TExpected, TAnd, TOr>(AssertionBuilder));

    public BaseAssertCondition<TActual?, TAnd, TOr> AssignableTo<TExpected>() => Wrap(new DelegateAssertCondition<TActual?,TExpected,TAnd,TOr>(AssertionBuilder,
        default,
        (value, _, _) => value!.GetType().IsAssignableTo(typeof(TExpected)),
        (actual, _) => $"{actual?.GetType()} is not assignable to {typeof(TExpected).Name}"));

    public BaseAssertCondition<TActual?, TAnd, TOr> AssignableFrom<TExpected>() => Wrap(new DelegateAssertCondition<TActual?,TExpected,TAnd,TOr>(AssertionBuilder,
        default,
        (value, _, _) => value!.GetType().IsAssignableFrom(typeof(TExpected)),
        (actual, _) => $"{actual?.GetType()} is not assignable from {typeof(TExpected).Name}"));
    
    public IsNot<TActual?, TAnd, TOr> Not => new(AssertionBuilder, ConnectorType, OtherAssertCondition);
}