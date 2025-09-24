#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class GenericExtensions
{
    #region Type Assertions
    
    public static AssertionBuilder<object> IsTypeOf(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new TypeOfExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static CastedAssertionBuilder<object, TExpected> IsTypeOf<TExpected>(this IValueSource<object> valueSource)
    {
        return new CastedAssertionBuilder<object, TExpected>(IsTypeOf(valueSource, typeof(TExpected)));
    }

    public static AssertionBuilder<object> IsAssignableTo(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new AssignableToExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static CastedAssertionBuilder<object, TExpected> IsAssignableTo<TExpected>(this IValueSource<object> valueSource)
    {
        return new CastedAssertionBuilder<object, TExpected>(IsAssignableTo(valueSource, typeof(TExpected)));
    }

    public static AssertionBuilder<object> IsAssignableFrom(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new AssignableFromExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static AssertionBuilder<object> IsAssignableFrom<TExpected>(this IValueSource<object> valueSource)
    {
        return IsAssignableFrom(valueSource, typeof(TExpected));
    }

    public static AssertionBuilder<object> IsNotTypeOf(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new NotTypeOfExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static AssertionBuilder<object> IsNotTypeOf<TExpected>(this IValueSource<object> valueSource)
    {
        return valueSource.RegisterAssertion(new NotTypeOfExpectedValueAssertCondition<object>(typeof(TExpected))
            , [typeof(TExpected).Name]);
    }

    public static AssertionBuilder<object> IsNotAssignableTo(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new NotAssignableToExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static AssertionBuilder<object> IsNotAssignableTo<TExpected>(this IValueSource<object> valueSource)
    {
        return valueSource.RegisterAssertion(new NotAssignableToExpectedValueAssertCondition<object>(typeof(TExpected))
            , [typeof(TExpected).Name]);
    }

    public static AssertionBuilder<object> IsNotAssignableFrom(this IValueSource<object> valueSource, Type type)
    {
        return valueSource.RegisterAssertion(new AssignableFromExpectedValueAssertCondition<object>(type)
            , [type.Name]);
    }

    public static AssertionBuilder<object> IsNotAssignableFrom<TExpected>(this IValueSource<object> valueSource)
    {
        return valueSource.RegisterAssertion(new AssignableFromExpectedValueAssertCondition<object>(typeof(TExpected))
            , [typeof(TExpected).Name]);
    }
    
    #endregion
    
    #region Equality Assertions
    
    public static GenericEqualToAssertion<TActual> IsEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return IsEqualTo(valueSource, expected, EqualityComparer<TActual>.Default, doNotPopulateThisValue1);
    }

    public static GenericEqualToAssertion<TActual> IsEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, IEqualityComparer<TActual> equalityComparer, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        var assertionBuilder = valueSource
            .RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(expected, equalityComparer), [doNotPopulateThisValue1]);

        return new GenericEqualToAssertion<TActual>(assertionBuilder);
    }

    public static AssertionBuilder<TActual> IsEquatableOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return valueSource
            .RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(expected), [doNotPopulateThisValue1]);
    }

    public static EquivalentToAssertion<TActual> IsEquivalentTo<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TActual,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TExpected>(this IValueSource<TActual> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        var assertionBuilder = valueSource.RegisterAssertion(new EquivalentToExpectedValueAssertCondition<TActual, TExpected>(expected, doNotPopulateThisValue1)
            , [doNotPopulateThisValue1]);

        return new EquivalentToAssertion<TActual>(assertionBuilder);
    }

    public static AssertionBuilder<TActual> IsSameReferenceAs<TActual, TExpected>(this IValueSource<TActual> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return valueSource.RegisterAssertion(new SameReferenceExpectedValueAssertCondition<TActual, TExpected>(expected)
            , [doNotPopulateThisValue1]);
    }

    public static AssertionBuilder<TActual> IsNull<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NullExpectedValueAssertCondition<TActual>()
            , []);
    }
    
    public static GenericNotEqualToAssertion<TActual> IsNotEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        var assertionBuilder = valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);

        return new GenericNotEqualToAssertion<TActual>(assertionBuilder);
    }

    public static NotNullAssertion<TActual> IsNotNull<TActual>(this IValueSource<TActual> valueSource) where TActual : class
    {
        return new NotNullAssertion<TActual>(valueSource.RegisterConversionAssertion(new NotNullExpectedValueAssertCondition<TActual>(), []));
    }

    public static AssertionBuilder<TActual> IsNotNull<TActual>(this IValueSource<TActual?> valueSource) where TActual : struct
    {
        return valueSource.RegisterConversionAssertion(new NotNullStructExpectedValueAssertCondition<TActual>(), []);
    }

    public static AssertionBuilder<TActual> IsNotEquatableOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);
    }

    public static NotEquivalentToAssertion<TActual> IsNotEquivalentTo<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TActual,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TExpected>(this IValueSource<TActual> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        var assertionBuilder = valueSource.RegisterAssertion(new NotEquivalentToExpectedValueAssertCondition<TActual, TExpected>(expected, doNotPopulateThisValue1)
            , [doNotPopulateThisValue1]);

        return new NotEquivalentToAssertion<TActual>(assertionBuilder);
    }

    public static AssertionBuilder<TActual> IsNotSameReferenceAs<TActual, TExpected>(this IValueSource<TActual> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return valueSource.RegisterAssertion(new NotSameReferenceExpectedValueAssertCondition<TActual, TExpected>(expected)
            , [doNotPopulateThisValue1]);
    }

    public static AssertionBuilder<TActual> IsDefault<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new DefaultExpectedValueAssertCondition<TActual>()
            , []);
    }

    public static AssertionBuilder<TActual> IsNotDefault<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NotDefaultExpectedValueAssertCondition<TActual>()
            , []);
    }
    
    #endregion
    
    #region Satisfies and Collections
    
    public static AssertionBuilder<TActual> Satisfies<TActual, TExpected>(
        this IValueSource<TActual> valueSource, Func<TActual, TExpected> mapper,
        Func<IValueSource<TExpected>, AssertionBuilder<TExpected>> assert,
        [CallerArgumentExpression(nameof(mapper))] string mapperExpression = "",
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        return valueSource.RegisterAssertion(new SatisfiesAssertCondition<TActual, TExpected>(t => Task.FromResult(mapper(t)), assert, mapperExpression, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }

    public static AssertionBuilder<TActual> Satisfies<TActual, TExpected>(
        this IValueSource<TActual> valueSource, Func<TActual, Task<TExpected>> asyncMapper,
        Func<IValueSource<TExpected>, AssertionBuilder<TExpected>> assert,
        [CallerArgumentExpression(nameof(asyncMapper))] string mapperExpression = "",
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        return valueSource.RegisterAssertion(new SatisfiesAssertCondition<TActual, TExpected>(asyncMapper, assert, mapperExpression, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }

    public static CollectionWrapper<TInner> All<TInner>(this IValueSource<IEnumerable<TInner>> valueSource)
    {
        return new(valueSource);
    }
    
    #endregion
    
    #region IsIn / Contains

    public static AssertionBuilder<TActual> IsIn<TActual>(this IValueSource<TActual> valueSource, params TActual[] expected)
    {
        return IsIn(valueSource, expected, EqualityComparer<TActual>.Default);
    }

    public static AssertionBuilder<TActual> IsIn<TActual>(this IValueSource<TActual> valueSource, IEnumerable<TActual> expected)
    {
        return IsIn(valueSource, expected, EqualityComparer<TActual>.Default);
    }

    public static AssertionBuilder<TActual> IsIn<TActual>(this IValueSource<TActual> valueSource, IEnumerable<TActual> expected, IEqualityComparer<TActual> equalityComparer)
    {
        var expectedArray = expected as TActual[] ?? expected.ToArray();
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual[]>(
            expectedArray,
            (actual, expectedValues, _) =>
            {
                if (actual == null && expectedValues.Any(e => e == null))
                {
                    return true;
                }
                if (actual == null)
                {
                    return false;
                }
                return expectedValues.Contains(actual, equalityComparer);
            },
            (actual, expectedValues, _) => $"{actual} was not found in the expected values: [{string.Join(", ", expectedValues)}]",
            $"be in [{string.Join(", ", expectedArray)}]"
        ), [$"[{string.Join(", ", expectedArray)}]"]);
    }

    public static AssertionBuilder<TActual> IsNotIn<TActual>(this IValueSource<TActual> valueSource, params TActual[] expected)
    {
        return IsNotIn(valueSource, expected, EqualityComparer<TActual>.Default);
    }

    public static AssertionBuilder<TActual> IsNotIn<TActual>(this IValueSource<TActual> valueSource, IEnumerable<TActual> expected)
    {
        return IsNotIn(valueSource, expected, EqualityComparer<TActual>.Default);
    }

    public static AssertionBuilder<TActual> IsNotIn<TActual>(this IValueSource<TActual> valueSource, IEnumerable<TActual> expected, IEqualityComparer<TActual> equalityComparer)
    {
        var expectedArray = expected as TActual[] ?? expected.ToArray();
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual[]>(
            expectedArray,
            (actual, expectedValues, _) =>
            {
                if (actual == null && expectedValues.All(e => e != null))
                {
                    return true;
                }
                if (actual == null)
                {
                    return false;
                }
                return !expectedValues.Contains(actual, equalityComparer);
            },
            (actual, expectedValues, _) => $"{actual} was found in the values: [{string.Join(", ", expectedValues)}]",
            $"not be in [{string.Join(", ", expectedArray)}]"
        ), [$"[{string.Join(", ", expectedArray)}]"]);
    }

    #endregion
}
