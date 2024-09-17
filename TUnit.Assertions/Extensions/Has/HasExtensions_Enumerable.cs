#nullable disable

using System.Collections;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static AssertionBuilder<TActual, TAnd, TOr> HasSingleItem<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, IEqualityComparer equalityComparer = null) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(null),
            1)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> HasDistinctItems<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableDistinctItemsAssertCondition<TActual, object, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(null),
            default,
            null)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> HasDistinctItems<TActual, TInner, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, IEqualityComparer<TInner> equalityComparer) 
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableDistinctItemsAssertCondition<TActual, TInner, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(null),
            default,
            equalityComparer)
            .ChainedTo(assertionBuilder);
    }
    
    public static EnumerableCount<TActual, TAnd, TOr> HasCount<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableCount<TActual, TAnd, TOr>((AssertionBuilder<TActual, TAnd, TOr>)assertionBuilder.AppendCallerMethod(null));
    }
}