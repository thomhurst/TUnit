using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions;

public class IsNot<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }
    
    public IsNot(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Not");
    }
    
    public BaseAssertCondition<TActual, TAnd, TOr> Null()
    {
        return Combine(new NotNullAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(string.Empty)));
    }
    
    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") => Combine(new NotEqualsAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected));
    
    public BaseAssertCondition<TActual, TAnd, TOr> TypeOf<TExpected>() => Combine(new NotTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName)));
    
    public BaseAssertCondition<TActual, TAnd, TOr> AssignableTo<TExpected>() => Combine(new DelegateAssertCondition<TActual,TExpected,TAnd,TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
        default,
        (value, _, _, _) => !value!.GetType().IsAssignableTo(typeof(TExpected)),
        (actual, _) => $"{actual?.GetType()} is assignable to {typeof(TExpected).Name}"));

    public BaseAssertCondition<TActual, TAnd, TOr> AssignableFrom<TExpected>() => Combine(new DelegateAssertCondition<TActual,TExpected,TAnd,TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
        default,
        (value, _, _, _) => !value!.GetType().IsAssignableFrom(typeof(TExpected)),
        (actual, _) => $"{actual?.GetType()} is assignable from {typeof(TExpected).Name}"));
}