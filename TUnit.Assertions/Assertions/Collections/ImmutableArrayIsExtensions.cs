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
    
    [SuppressMessage("Usage", "TUnitAssertions0003:Compiler argument populated")]
    public static class ImmutableArrayIsExtensions
    {
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsEquivalentTo<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            TInner>(this IValueSource<ImmutableArray<TInner>> valueSource, ImmutableArray<TInner> expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        {
            return IsEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), doNotPopulateThisValue);
        }
    
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsEquivalentTo<
            TInner>(this IValueSource<ImmutableArray<TInner>> valueSource,
            ImmutableArray<TInner> expected, IEqualityComparer<TInner> comparer,
            [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        {
            return IsEquivalentTo(valueSource, expected, comparer, CollectionOrdering.Matching, doNotPopulateThisValue);
        }
    
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsEquivalentTo<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            TInner>(this IValueSource<ImmutableArray<TInner>> valueSource, ImmutableArray<TInner> expected, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        {
            return IsEquivalentTo(valueSource, expected, new CollectionEquivalentToEqualityComparer<TInner>(), collectionOrdering, doNotPopulateThisValue);
        }
        
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsEquivalentTo<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource, ImmutableArray<TInner> expected, IEqualityComparer<TInner> comparer, CollectionOrdering collectionOrdering, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        {
            return valueSource.RegisterAssertion(
                new EnumerableEquivalentToExpectedValueAssertCondition<ImmutableArray<TInner>, TInner>(expected,
                    comparer, collectionOrdering), [doNotPopulateThisValue]);
        }
    
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsInOrder<TInner>(
                this IValueSource<ImmutableArray<TInner>> valueSource)
        {
            return IsOrderedBy(valueSource, x => x, Comparer<TInner>.Default, null);
        }
        
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsInDescendingOrder<TInner>(
            this IValueSource<ImmutableArray<TInner>> valueSource)
        {
            return IsOrderedByDescending(valueSource, x => x, Comparer<TInner>.Default, null);
        }
        
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsInOrder<TInner>(
            this IValueSource<ImmutableArray<TInner>> valueSource,
            IComparer<TInner> comparer)
        {
            return IsOrderedBy(valueSource, x => x, comparer, null);
        }
        
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsInDescendingOrder<TInner>(
            this IValueSource<ImmutableArray<TInner>> valueSource,
            IComparer<TInner> comparer)
        {
            return IsOrderedByDescending(valueSource, x => x, comparer, null);
        }
        
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsOrderedBy<TInner, TComparisonItem>(
            this IValueSource<ImmutableArray<TInner>> valueSource,
            Func<TInner, TComparisonItem> comparisonItemSelector,
            [CallerArgumentExpression(nameof(comparisonItemSelector))] string doNotPopulateThisValue = null)
        {
            return IsOrderedBy(valueSource, comparisonItemSelector, Comparer<TComparisonItem>.Default, doNotPopulateThisValue);
        }
        
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsOrderedByDescending<TInner, TComparisonItem>(
            this IValueSource<ImmutableArray<TInner>> valueSource,
            Func<TInner, TComparisonItem> comparisonItemSelector,
            [CallerArgumentExpression(nameof(comparisonItemSelector))] string doNotPopulateThisValue = null)
        {
            return IsOrderedByDescending(valueSource, comparisonItemSelector, Comparer<TComparisonItem>.Default, doNotPopulateThisValue);
        }
        
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsOrderedBy<TInner, TComparisonItem>(
            this IValueSource<ImmutableArray<TInner>> valueSource,
            Func<TInner, TComparisonItem> comparisonItemSelector,
            IComparer<TComparisonItem> comparer,
            [CallerArgumentExpression(nameof(comparisonItemSelector))] string doNotPopulateThisValue = null,
            [CallerArgumentExpression(nameof(comparer))] string doNotPopulateThisValue2 = null)
        {
            return valueSource.RegisterAssertion(
                new EnumerableOrderedByAssertCondition<ImmutableArray<TInner>, TInner, TComparisonItem>(comparer, comparisonItemSelector, Order.Ascending), [doNotPopulateThisValue, doNotPopulateThisValue2]);
        }
        
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsOrderedByDescending<TInner, TComparisonItem>(
            this IValueSource<ImmutableArray<TInner>> valueSource,
            Func<TInner, TComparisonItem> comparisonItemSelector,
            IComparer<TComparisonItem> comparer,
            [CallerArgumentExpression(nameof(comparisonItemSelector))] string doNotPopulateThisValue = null,
            [CallerArgumentExpression(nameof(comparer))] string doNotPopulateThisValue2 = null)
        {
            return valueSource.RegisterAssertion(
                new EnumerableOrderedByAssertCondition<ImmutableArray<TInner>, TInner, TComparisonItem>(comparer, comparisonItemSelector, Order.Descending), [doNotPopulateThisValue, doNotPopulateThisValue2]);
        }
    
        public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> IsEmpty<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource)
        {
            return valueSource.RegisterAssertion(new EnumerableCountEqualToExpectedValueAssertCondition<ImmutableArray<TInner>, TInner>(0), []);
        }
    }