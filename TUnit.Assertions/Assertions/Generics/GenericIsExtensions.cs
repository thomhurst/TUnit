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
    public static InvokableValueAssertion<object> IsTypeOf(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new TypeOfExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static CastedAssertionBuilder<object, TExpected> IsTypeOf<TExpected>(this IValueSource<object> valueSource)
    {
        return new CastedAssertionBuilder<object, TExpected>(IsTypeOf(valueSource, typeof(TExpected)));
    }

    public static InvokableValueAssertion<object> IsAssignableTo(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new AssignableToExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static CastedAssertionBuilder<object, TExpected> IsAssignableTo<TExpected>(this IValueSource<object> valueSource)
    {
        return new CastedAssertionBuilder<object, TExpected>(IsAssignableTo(valueSource, typeof(TExpected)));
    }


    public static InvokableValueAssertion<object> IsAssignableFrom(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new AssignableFromExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static InvokableValueAssertion<object> IsAssignableFrom<TExpected>(this IValueSource<object> valueSource)
    {
        return IsAssignableFrom(valueSource, typeof(TExpected));
    }

    public static InvokableValueAssertion<object> IsNotTypeOf(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new NotTypeOfExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static InvokableValueAssertion<object> IsNotTypeOf<TExpected>(this IValueSource<object> valueSource)
    {
        return valueSource.RegisterAssertion(new NotTypeOfExpectedValueAssertCondition<object>(typeof(TExpected))
            , [typeof(TExpected).Name]);
    }

    public static InvokableValueAssertion<object> IsNotAssignableTo(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new NotAssignableToExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static InvokableValueAssertion<object> IsNotAssignableTo<TExpected>(this IValueSource<object> valueSource)
    {
        return valueSource.RegisterAssertion(new NotAssignableToExpectedValueAssertCondition<object>(typeof(TExpected))
            , [typeof(TExpected).Name]);
    }

    public static InvokableValueAssertion<object> IsNotAssignableFrom(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new AssignableFromExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static InvokableValueAssertion<object> IsNotAssignableFrom<TExpected>(this IValueSource<object> valueSource)
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

    public static InvokableValueAssertion<TActual> IsEquatableOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return valueSource
            .RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(expected), [doNotPopulateThisValue1]);
    }

    public static EquivalentToAssertionBuilderWrapper<TActual, TExpected> IsEquivalentTo<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TActual,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TExpected>(this IValueSource<TActual> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        var assertionBuilder = valueSource.RegisterAssertion(new EquivalentToExpectedValueAssertCondition<TActual, TExpected>(expected, doNotPopulateThisValue1)
            , [doNotPopulateThisValue1]);

        return new EquivalentToAssertionBuilderWrapper<TActual, TExpected>(assertionBuilder);
    }

    public static InvokableValueAssertion<TActual> IsSameReferenceAs<TActual, TExpected>(this IValueSource<TActual> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return valueSource.RegisterAssertion(new SameReferenceExpectedValueAssertCondition<TActual, TExpected>(expected)
            , [doNotPopulateThisValue1]);
    }

    public static InvokableValueAssertion<TActual> IsNull<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NullExpectedValueAssertCondition<TActual>()
            , []);
    }
}
