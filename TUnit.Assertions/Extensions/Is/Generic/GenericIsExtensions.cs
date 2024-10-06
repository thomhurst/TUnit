#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Generic;

public static class GenericIsExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsEquatableOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return valueSource
            .RegisterAssertion(new EqualsAssertCondition<TActual>(expected), [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return valueSource.RegisterAssertion(new EquivalentToAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<object> IsSameReference(this IValueSource<object> valueSource, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return valueSource.RegisterAssertion(new SameReferenceAssertCondition<object, object>(expected)
            , [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNull<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NullAssertCondition<TActual>()
            , []);
    }

    public static InvokableValueAssertionBuilder<TActual> IsTypeOf<TActual>(this IValueSource<TActual> valueSource, Type type)  
    {
        return valueSource.RegisterAssertion(new TypeOfAssertCondition<TActual>(type)
            , [type.Name]);
    }

    public static InvokableValueAssertionBuilder<TActual> IsAssignableTo<TActual>(this IValueSource<TActual> valueSource, Type type) 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, Type>(default,
            (value, _, _, _) => value!.GetType().IsAssignableTo(type),
            (actual, _, _) => $"{actual?.GetType()} is not assignable to {type.Name}")
            , [type.Name]); }

    public static InvokableValueAssertionBuilder<TActual> IsAssignableFrom<TActual>(this IValueSource<TActual> valueSource, Type type) 
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, Type>(default,
            (value, _, _, _) => value!.GetType().IsAssignableFrom(type),
            (actual, _, _) => $"{actual?.GetType()} is not assignable from {type.Name}")
            , [type.Name]); }
}