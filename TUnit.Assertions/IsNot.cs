using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public class IsNot<TActual, TAnd, TOr> : NotConnector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public IsNot(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Not");
    }
    
    public BaseAssertCondition<TActual, TAnd, TOr> Null()
    {
        return Wrap(new NotNullAssertCondition<TActual, TAnd, TOr>(AssertionBuilder));
    }
    
    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(TActual expected, [CallerArgumentExpression("expected")] string expectedExpression = "") => Invert(new EqualsAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(expectedExpression), expected),
        (actual, _) => $"Expected {actual} to equal {expected}");
    
    public BaseAssertCondition<TActual, TAnd, TOr> TypeOf<TExpected>() => Invert(new TypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder),
        (actual, _) => $"Expected {actual} to not be of type {typeof(TExpected)}");
    
    public BaseAssertCondition<TActual, TAnd, TOr> AssignableTo<TExpected>() => Wrap(new DelegateAssertCondition<TActual,TExpected,TAnd,TOr>(AssertionBuilder,
        default,
        (value, _, _) => !value!.GetType().IsAssignableTo(typeof(TExpected)),
        (actual, _) => $"{actual?.GetType()} is assignable to {typeof(TExpected).Name}"));

    public BaseAssertCondition<TActual, TAnd, TOr> AssignableFrom<TExpected>() => Wrap(new DelegateAssertCondition<TActual,TExpected,TAnd,TOr>(AssertionBuilder,
        default,
        (value, _, _) => !value!.GetType().IsAssignableFrom(typeof(TExpected)),
        (actual, _) => $"{actual?.GetType()} is assignable from {typeof(TExpected).Name}"));
}