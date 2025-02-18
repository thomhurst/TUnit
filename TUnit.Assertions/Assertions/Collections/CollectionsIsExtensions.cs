#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Equality;

namespace TUnit.Assertions.Extensions;

[SuppressMessage("Usage", "TUnitAssertions0003:Compiler argument populated")]
public static class CollectionsIsExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<TActual,  
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return IsEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), doNotPopulateThisValue);
    }

    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<
        TActual,
        TInner>(this IValueSource<TActual> valueSource,
        IEnumerable<TInner> expected, IEqualityComparer<TInner> comparer,
        [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return IsEquivalentTo(valueSource, expected, comparer, CollectionOrdering.Matching, doNotPopulateThisValue);
    }

    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<TActual,  
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return IsEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), collectionOrdering, doNotPopulateThisValue);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<TActual, TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, IEqualityComparer<TInner> comparer, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return valueSource.RegisterAssertion(
            new EnumerableEquivalentToExpectedValueAssertCondition<TActual, TInner>(expected,
                comparer, collectionOrdering), [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsInOrder<TInner>(
            this IValueSource<IEnumerable<TInner>> valueSource)
    {
        return IsOrderedBy(valueSource, x => x, Comparer<TInner>.Default, null);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsInDescendingOrder<TInner>(
        this IValueSource<IEnumerable<TInner>> valueSource)
    {
        return IsOrderedByDescending(valueSource, x => x, Comparer<TInner>.Default, null);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsInOrder<TInner>(
        this IValueSource<IEnumerable<TInner>> valueSource,
        IComparer<TInner> comparer)
    {
        return IsOrderedBy(valueSource, x => x, comparer, null);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsInDescendingOrder<TInner>(
        this IValueSource<IEnumerable<TInner>> valueSource,
        IComparer<TInner> comparer)
    {
        return IsOrderedByDescending(valueSource, x => x, comparer, null);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsOrderedBy<TInner, TComparisonItem>(
        this IValueSource<IEnumerable<TInner>> valueSource,
        Func<TInner, TComparisonItem> comparisonItemSelector,
        [CallerArgumentExpression(nameof(comparisonItemSelector))] string doNotPopulateThisValue = null)
    {
        return IsOrderedBy(valueSource, comparisonItemSelector, Comparer<TComparisonItem>.Default, doNotPopulateThisValue);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsOrderedByDescending<TInner, TComparisonItem>(
        this IValueSource<IEnumerable<TInner>> valueSource,
        Func<TInner, TComparisonItem> comparisonItemSelector,
        [CallerArgumentExpression(nameof(comparisonItemSelector))] string doNotPopulateThisValue = null)
    {
        return IsOrderedByDescending(valueSource, comparisonItemSelector, Comparer<TComparisonItem>.Default, doNotPopulateThisValue);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsOrderedBy<TInner, TComparisonItem>(
        this IValueSource<IEnumerable<TInner>> valueSource,
        Func<TInner, TComparisonItem> comparisonItemSelector,
        IComparer<TComparisonItem> comparer,
        [CallerArgumentExpression(nameof(comparisonItemSelector))] string doNotPopulateThisValue = null,
        [CallerArgumentExpression(nameof(comparer))] string doNotPopulateThisValue2 = null)
    {
        return valueSource.RegisterAssertion(
            new EnumerableOrderedByAssertCondition<TInner, TComparisonItem>(comparer, comparisonItemSelector, Order.Ascending), [doNotPopulateThisValue, doNotPopulateThisValue2]);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsOrderedByDescending<TInner, TComparisonItem>(
        this IValueSource<IEnumerable<TInner>> valueSource,
        Func<TInner, TComparisonItem> comparisonItemSelector,
        IComparer<TComparisonItem> comparer,
        [CallerArgumentExpression(nameof(comparisonItemSelector))] string doNotPopulateThisValue = null,
        [CallerArgumentExpression(nameof(comparer))] string doNotPopulateThisValue2 = null)
    {
        return valueSource.RegisterAssertion(
            new EnumerableOrderedByAssertCondition<TInner, TComparisonItem>(comparer, comparisonItemSelector, Order.Descending), [doNotPopulateThisValue, doNotPopulateThisValue2]);
    }

    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> IsEmpty<TInner>(this IValueSource<IEnumerable<TInner>> valueSource)
    {
        return valueSource.RegisterAssertion(new EnumerableCountEqualToExpectedValueAssertCondition<TInner>(0), []);
    }
}