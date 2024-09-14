using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IIs<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal Is<TActual, TAnd, TOr> Is();
    internal IsNot<TActual, TAnd, TOr> IsNot();
    
    public BaseAssertCondition<TActual, TAnd, TOr> IsSameReference(TActual expected) => Is().SameReference(expected);
    public BaseAssertCondition<TActual, TAnd, TOr> IsNull<TInner>(IEnumerable<TInner> expected) => Is().Null();
    public BaseAssertCondition<TActual, TAnd, TOr> IsAssignableFrom<T>() where T : TActual => Is().AssignableFrom<T>();
    public BaseAssertCondition<TActual, TAnd, TOr> IsAssignableTo<T>() where T : TActual => Is().AssignableFrom<T>();
    public BaseAssertCondition<TActual, TAnd, TOr> IsTypeOf<T>() where T : TActual => Is().TypeOf<T>();
    
    public BaseAssertCondition<TActual, TAnd, TOr> IsNotEqualTo(TActual expected) => IsNot().EqualTo(expected);
    public BaseAssertCondition<TActual, TAnd, TOr> IsNotNull<TInner>(IEnumerable<TInner> expected) => IsNot().Null();
    public BaseAssertCondition<TActual, TAnd, TOr> IsNotAssignableFrom<T>() where T : TActual => IsNot().AssignableFrom<T>();
    public BaseAssertCondition<TActual, TAnd, TOr> IsNotAssignableTo<T>() where T : TActual => IsNot().AssignableFrom<T>();
    public BaseAssertCondition<TActual, TAnd, TOr> IsNotTypeOf<T>() where T : TActual => IsNot().TypeOf<T>();
}