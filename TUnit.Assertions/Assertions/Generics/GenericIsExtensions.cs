#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class GenericIsExtensions
{
    public static InvokableValueAssertionBuilder<object> IsTypeOf(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new TypeOfExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static CastableAssertionBuilder<object, TExpected> IsTypeOf<TExpected>(this IValueSource<object> valueSource)
    {
        return new CastableAssertionBuilder<object, TExpected>(IsTypeOf(valueSource, typeof(TExpected)));
    }

    public static InvokableValueAssertionBuilder<object> IsAssignableTo(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new AssignableToExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static CastableAssertionBuilder<object, TExpected> IsAssignableTo<TExpected>(this IValueSource<object> valueSource)
    {
        return new CastableAssertionBuilder<object, TExpected>(IsAssignableTo(valueSource, typeof(TExpected)));
    }


    public static InvokableValueAssertionBuilder<object> IsAssignableFrom(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new AssignableFromExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static InvokableValueAssertionBuilder<object> IsAssignableFrom<TExpected>(this IValueSource<object> valueSource)
    {
        return IsAssignableFrom(valueSource, typeof(TExpected));
    }

    public static InvokableValueAssertionBuilder<object> IsNotTypeOf(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new NotTypeOfExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static InvokableValueAssertionBuilder<object> IsNotTypeOf<TExpected>(this IValueSource<object> valueSource)
    {
        return valueSource.RegisterAssertion(new NotTypeOfExpectedValueAssertCondition<object>(typeof(TExpected))
            , [typeof(TExpected).Name]);
    }

    public static InvokableValueAssertionBuilder<object> IsNotAssignableTo(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new NotAssignableToExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static InvokableValueAssertionBuilder<object> IsNotAssignableTo<TExpected>(this IValueSource<object> valueSource)
    {
        return valueSource.RegisterAssertion(new NotAssignableToExpectedValueAssertCondition<object>(typeof(TExpected))
            , [typeof(TExpected).Name]);
    }

    public static InvokableValueAssertionBuilder<object> IsNotAssignableFrom(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new AssignableFromExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static InvokableValueAssertionBuilder<object> IsNotAssignableFrom<TExpected>(this IValueSource<object> valueSource)
    {
        return valueSource.RegisterAssertion(new AssignableFromExpectedValueAssertCondition<object>(typeof(TExpected))
            , [typeof(TExpected).Name]);
    }
    public static GenericEqualToAssertionBuilderWrapper<TActual> IsEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return IsEqualTo(valueSource, expected, EqualityComparer<TActual>.Default, doNotPopulateThisValue1);
    }
    
    public static GenericEqualToAssertionBuilderWrapper<TActual> IsEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, IEqualityComparer<TActual> equalityComparer, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        var assertionBuilder = valueSource
            .RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(expected, equalityComparer), [doNotPopulateThisValue1]);
        
        return new GenericEqualToAssertionBuilderWrapper<TActual>(assertionBuilder);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsEquatableOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return valueSource
            .RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(expected), [doNotPopulateThisValue1]);
    }
    
    public static EquivalentToAssertionBuilderWrapper<TActual, TExpected> IsEquivalentTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TExpected>(this IValueSource<TActual> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        var assertionBuilder = valueSource.RegisterAssertion(new EquivalentToExpectedValueAssertCondition<TActual, TExpected>(expected, doNotPopulateThisValue1)
            , [doNotPopulateThisValue1]);
        
        return new EquivalentToAssertionBuilderWrapper<TActual, TExpected>(assertionBuilder);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsSameReferenceAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TExpected>(this IValueSource<TActual> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return valueSource.RegisterAssertion(new SameReferenceExpectedValueAssertCondition<TActual, TExpected>(expected)
            , [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNull<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NullExpectedValueAssertCondition<TActual>()
            , []);
    }
}