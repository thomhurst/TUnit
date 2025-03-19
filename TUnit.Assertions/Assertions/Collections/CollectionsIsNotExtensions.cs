#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Equality;

namespace TUnit.Assertions.Extensions;

public static class CollectionsIsNotExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsNotEquivalentTo<TActual,  
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return IsNotEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), doNotPopulateThisValue, null);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNotEquivalentTo<
        TActual,
        TInner>(this IValueSource<TActual> valueSource,
        IEnumerable<TInner> expected, IEqualityComparer<TInner> comparer,
        [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null,
        [CallerArgumentExpression(nameof(comparer))] string doNotPopulateThisValue2 = null)
        where TActual : IEnumerable<TInner>
    {
        return IsNotEquivalentTo(valueSource, expected, comparer, CollectionOrdering.Matching, doNotPopulateThisValue, doNotPopulateThisValue2);
    }

    public static InvokableValueAssertionBuilder<TActual> IsNotEquivalentTo<TActual,  
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null, [CallerArgumentExpression(nameof(collectionOrdering))] string doNotPopulateThisValue2 = null)
        where TActual : IEnumerable<TInner>
    {
        return IsNotEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), collectionOrdering, doNotPopulateThisValue, doNotPopulateThisValue2);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotEquivalentTo<TActual, TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, IEqualityComparer<TInner> comparer, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null, [CallerArgumentExpression(nameof(collectionOrdering))] string doNotPopulateThisValue2 = null)
        where TActual : IEnumerable<TInner>
    {
        return valueSource.RegisterAssertion(
            new EnumerableNotEquivalentToExpectedValueAssertCondition<TActual, TInner>(expected,
                comparer, collectionOrdering), [doNotPopulateThisValue, doNotPopulateThisValue2]);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsNotEmpty<TInner>(this IValueSource<IEnumerable<TInner>> valueSource)
    {
        return valueSource.RegisterAssertion(new EnumerableCountNotEqualToExpectedValueAssertCondition<IEnumerable<TInner>, TInner>(0)
            , []);
    }
}