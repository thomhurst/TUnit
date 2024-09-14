using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions;

public class Is<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }

    public Is(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder
            .AppendConnector(connectorType)
            .AppendExpression("Is");
    }

    public BaseAssertCondition<TActual, TAnd, TOr> SameReference(TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new SameReferenceAssertCondition<TActual, TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected));
    }

    public BaseAssertCondition<TActual, TAnd, TOr> Null() => Combine(new NullAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(string.Empty)));

    public BaseAssertCondition<TActual, TAnd, TOr> TypeOf<TExpected>()
        where TExpected : TActual => Combine(new TypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName)));

    public BaseAssertCondition<TActual, TAnd, TOr> AssignableTo<TExpected>() 
    {
        return Combine(new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is not assignable to {typeof(TExpected).Name}"));
    }

    public BaseAssertCondition<TActual, TAnd, TOr> AssignableFrom<TExpected>() 
        where TExpected : TActual => Combine(new DelegateAssertCondition<TActual,TExpected,TAnd,TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
        default,
        (value, _, _, _) => value!.GetType().IsAssignableFrom(typeof(TExpected)),
        (actual, _) => $"{actual?.GetType()} is not assignable from {typeof(TExpected).Name}"));
    
    public IsNot<TActual, TAnd, TOr> Not => new(AssertionBuilder, ConnectorType, OtherAssertCondition);
}