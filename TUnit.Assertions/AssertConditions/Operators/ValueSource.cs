﻿using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;
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
        return this.RegisterAssertion(new AssignableToExpectedValueAssertCondition<TActual>(type)
            , [type.Name]);
    }

    public CastableAssertionBuilder<TActual, TExpected> IsAssignableTo<TExpected>()
    {
        return new CastableAssertionBuilder<TActual, TExpected>(IsAssignableTo(typeof(TExpected)));
    }


    public InvokableValueAssertionBuilder<TActual> IsAssignableFrom(Type type)
    {
        return this.RegisterAssertion(new AssignableFromExpectedValueAssertCondition<TActual>(type)
            , [type.Name]);
    }

    public InvokableValueAssertionBuilder<TActual> IsAssignableFrom<TExpected>()
    {
        return IsAssignableFrom(typeof(TExpected));
    }
}