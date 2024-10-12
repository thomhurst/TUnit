using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueSource<TActual>(AssertionBuilder<TActual> assertionBuilder) : IValueSource<TActual>
{
    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder { get; } = assertionBuilder;

    public InvokableValueAssertionBuilder<TActual> IsTypeOf(Type type)
    {
        return this.RegisterAssertion(new TypeOfExpectedValueAssertCondition<TActual>(type)
            , [type.Name]);
    }

    public CastableAssertionBuilder<TActual, TExpected> IsTypeOf<TExpected>()
    {
        return new CastableAssertionBuilder<TActual, TExpected>(IsTypeOf(typeof(TExpected)));
    }

    public InvokableValueAssertionBuilder<TActual> IsAssignableTo(Type type)
    {
        return this.RegisterAssertion(new FuncValueAssertCondition<TActual, Type>(default,
                (value, _, _) => value!.GetType().IsAssignableTo(type),
                (actual, _, _) => $"{actual?.GetType()} is not assignable to {type.Name}")
            , [type.Name]);
    }

    public CastableAssertionBuilder<TActual, TExpected> IsAssignableTo<TExpected>()
    {
        return new CastableAssertionBuilder<TActual, TExpected>(IsAssignableTo(typeof(TExpected)));
    }


    public InvokableValueAssertionBuilder<TActual> IsAssignableFrom(Type type)
    {
        return this.RegisterAssertion(new FuncValueAssertCondition<TActual, Type>(default,
                (value, _, _) => value!.GetType().IsAssignableFrom(type),
                (actual, _, _) => $"{actual?.GetType()} is not assignable from {type.Name}")
            , [type.Name]);
    }

    public InvokableValueAssertionBuilder<TActual> IsAssignableFrom<TExpected>()
    {
        return IsAssignableFrom(typeof(TExpected));
    }
}