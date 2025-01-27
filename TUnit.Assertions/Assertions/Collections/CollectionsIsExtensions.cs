#nullable disable

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Equality;

namespace TUnit.Assertions.Extensions;

public static class CollectionsIsExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return IsEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), doNotPopulateThisValue);
    }

    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TInner>(this IValueSource<TActual> valueSource,
        IEnumerable<TInner> expected, IEqualityComparer<TInner> comparer,
        [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return IsEquivalentTo(valueSource, expected, comparer, CollectionOrdering.Matching, doNotPopulateThisValue);
    }

    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return IsEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), collectionOrdering, doNotPopulateThisValue);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, IEqualityComparer<TInner> comparer, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return valueSource.RegisterAssertion(
            new EnumerableEquivalentToExpectedValueAssertCondition<TActual, TInner>(expected,
                comparer, collectionOrdering), [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsEmpty<TActual>(this IValueSource<TActual> valueSource)
        where TActual : IEnumerable
    {
        return valueSource.RegisterAssertion(new EnumerableCountEqualToExpectedValueAssertCondition<TActual>(0), []);
    }
}