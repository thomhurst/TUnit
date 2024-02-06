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
        AssertionBuilder = assertionBuilder;
    }
    
    public BaseAssertCondition<TActual, TAnd, TOr> Null()
    {
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
        return Wrap(new NotNullAssertCondition<TActual, TAnd, TOr>(AssertionBuilder!));
#pragma warning restore CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.
    }
    
    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(TActual expected) => Invert(new EqualsAssertCondition<TActual, TAnd, TOr>(AssertionBuilder, expected),
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