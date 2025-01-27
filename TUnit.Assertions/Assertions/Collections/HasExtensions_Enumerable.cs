#nullable disable

using System.Collections;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> HasSingleItem<TInner>(this IValueSource<IEnumerable<TInner>> valueSource, IEqualityComparer equalityComparer = null) 
    {
        return valueSource.RegisterAssertion(new EnumerableCountEqualToExpectedValueAssertCondition<IEnumerable<TInner>>(1)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> HasDistinctItems<TInner>(this IValueSource<IEnumerable<TInner>> valueSource) 
    {
        return valueSource.RegisterAssertion(new EnumerableDistinctItemsExpectedValueAssertCondition<IEnumerable<TInner>, object>(null)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<IEnumerable<TInner>> HasDistinctItems<TInner>(this IValueSource<IEnumerable<TInner>> valueSource, IEqualityComparer<TInner> equalityComparer) 
    {
        return valueSource.RegisterAssertion(new EnumerableDistinctItemsExpectedValueAssertCondition<IEnumerable<TInner>, TInner>(equalityComparer)
            , []);
    }
    
    public static EnumerableCount<TInner> HasCount<TInner>(this IValueSource<IEnumerable<TInner>> valueSource) 
    {
        valueSource.AppendExpression("HasCount()");
        return new EnumerableCount<TInner>(valueSource);
    }
}