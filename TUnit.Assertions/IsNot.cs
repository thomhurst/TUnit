using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public class IsNot<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
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
        return Wrap(new NotNullAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(string.Empty)));
    }
    
    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") => Wrap(new NotEqualsAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected));
    
    public BaseAssertCondition<TActual, TAnd, TOr> TypeOf<TExpected>() => Wrap(new NotTypeOfAssertCondition<TActual, TExpected, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName)));
    
    public BaseAssertCondition<TActual, TAnd, TOr> AssignableFrom<TExpected>() => Wrap(new DelegateAssertCondition<TActual,TExpected,TAnd,TOr>(AssertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
        default,
        (value, _, _, self) => !value!.GetType().IsAssignableFrom(typeof(TExpected)),
        (actual, _) => $"{actual?.GetType()} is assignable from {typeof(TExpected).Name}"));
}