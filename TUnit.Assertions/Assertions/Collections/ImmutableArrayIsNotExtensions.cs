#nullable disable

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Equality;

namespace TUnit.Assertions.Extensions;

public static class ImmutableArrayIsNotExtensions
{
    public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsNotEquivalentTo<  
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TInner>(this IValueSource<ImmutableArray<TInner>> valueSource, IEnumerable<TInner> expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return IsNotEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), doNotPopulateThisValue);
    }

    public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsNotEquivalentTo<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource,
        IEnumerable<TInner> expected, IEqualityComparer<TInner> comparer,
        [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null,
        [CallerArgumentExpression(nameof(comparer))] string doNotPopulateThisValue2 = null)
    {
        return IsNotEquivalentTo(valueSource, expected, comparer, CollectionOrdering.Matching, doNotPopulateThisValue, doNotPopulateThisValue2);
    }

    public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsNotEquivalentTo<  
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TInner>(this IValueSource<ImmutableArray<TInner>> valueSource, IEnumerable<TInner> expected, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null, [CallerArgumentExpression(nameof(collectionOrdering))] string doNotPopulateThisValue2 = null)
    {
        return IsNotEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), collectionOrdering, doNotPopulateThisValue, doNotPopulateThisValue2);
    }
    
    public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsNotEquivalentTo<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource, IEnumerable<TInner> expected, IEqualityComparer<TInner> comparer, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null, [CallerArgumentExpression(nameof(collectionOrdering))] string doNotPopulateThisValue2 = null)
    {
        return valueSource.RegisterAssertion(
            new EnumerableNotEquivalentToExpectedValueAssertCondition<ImmutableArray<TInner>, TInner>(expected,
                comparer, collectionOrdering), [doNotPopulateThisValue, doNotPopulateThisValue2]);
    }
    
    public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsNotEmpty<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource)
    {
        return valueSource.RegisterAssertion(new EnumerableCountNotEqualToExpectedValueAssertCondition<ImmutableArray<TInner>, TInner>(0)
            , []);
    }
}