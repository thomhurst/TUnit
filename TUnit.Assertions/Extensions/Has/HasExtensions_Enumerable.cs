#nullable disable

using System.Collections;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> HasSingleItem<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, IEqualityComparer equalityComparer = null) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr>(1)
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> HasDistinctItems<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableDistinctItemsAssertCondition<TActual, object, TAnd, TOr>(default,
            null)
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> HasDistinctItems<TActual, TInner, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, IEqualityComparer<TInner> equalityComparer) 
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableDistinctItemsAssertCondition<TActual, TInner, TAnd, TOr>(default,
            equalityComparer)
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
    
    public static EnumerableCount<TActual, TAnd, TOr> HasCount<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableCount<TActual, TAnd, TOr>(valueSource.AssertionBuilder.AppendCallerMethod(null));
    }
}