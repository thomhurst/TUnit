#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class GenericIsNotExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsNotNull<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource!.RegisterAssertion(new NotNullExpectedValueAssertCondition<TActual>()
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotEquatableOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNotTypeOf<TActual>(this IValueSource<TActual> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, Type>(default,
                (value, _, _) => value!.GetType() != type,
                (actual, _, _) => $"{actual?.GetType()} is type of {type.Name}")
            , [type.Name]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNotAssignableTo<TActual>(this IValueSource<TActual> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, Type>(default,
            (value, _, _) => !value!.GetType().IsAssignableTo(type),
            (actual, _, _) => $"{actual?.GetType()} is assignable to {type.Name}")
            , [type.Name]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNotAssignableFrom<TActual>(this IValueSource<TActual> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, Type>(default,
            (value, _, _) => !value!.GetType().IsAssignableFrom(type),
            (actual, _, _) => $"{actual?.GetType()} is assignable from {type.Name}")
            , [type.Name]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsDefault<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new DefaultExpectedValueAssertCondition<TActual>()
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotDefault<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NotDefaultExpectedValueAssertCondition<TActual>()
            , []);
    }
}