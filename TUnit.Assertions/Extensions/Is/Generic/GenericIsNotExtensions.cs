#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Generic;

public static class GenericIsNotExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsNotNull<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NotNullAssertCondition<TActual>()
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new NotEqualsAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNotTypeOf<TActual>(this IValueSource<TActual> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, Type>(default,
                (value, _, _, _) => value!.GetType() != type,
                (actual, _, _) => $"{actual?.GetType()} is type of {type.Name}")
            , [type.Name]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNotAssignableTo<TActual>(this IValueSource<TActual> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, Type>(default,
            (value, _, _, _) => !value!.GetType().IsAssignableTo(type),
            (actual, _, _) => $"{actual?.GetType()} is assignable to {type.Name}")
            , [type.Name]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNotAssignableFrom<TActual>(this IValueSource<TActual> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, Type>(default,
            (value, _, _, _) => !value!.GetType().IsAssignableFrom(type),
            (actual, _, _) => $"{actual?.GetType()} is assignable from {type.Name}")
            , [type.Name]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsDefault<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new DefaultAssertCondition<TActual>()
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotDefault<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NotDefaultAssertCondition<TActual>()
            , []);
    }
}