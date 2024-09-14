#nullable disable

using System.Collections;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> HasSingleItem<TActual, TAnd, TOr>(this IHas<TActual, TAnd, TOr> has, IEqualityComparer equalityComparer = null) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(has.Has(), new EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr>(
            has.Has().AssertionBuilder.AppendCallerMethod(null),
            1)
        );
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> HasDistinctItems<TActual, TAnd, TOr>(this IHas<TActual, TAnd, TOr> has) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(has.Has(), new EnumerableDistinctItemsAssertCondition<TActual, object, TAnd, TOr>(
            has.Has().AssertionBuilder.AppendCallerMethod(null),
            default,
            null)
        );
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> HasDistinctItems<TActual, TInner, TAnd, TOr>(this IHas<TActual, TAnd, TOr> has, IEqualityComparer<TInner> equalityComparer) 
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return AssertionConditionCombiner.Combine(has.Has(), new EnumerableDistinctItemsAssertCondition<TActual, TInner, TAnd, TOr>(
            has.Has().AssertionBuilder.AppendCallerMethod(null),
            default,
            equalityComparer)
        );
    }
    
    public static EnumerableCount<TActual, TAnd, TOr> HasCount<TActual, TAnd, TOr>(this IHas<TActual, TAnd, TOr> has) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new EnumerableCount<TActual, TAnd, TOr>(has.Has().AssertionBuilder.AppendCallerMethod(null), has.Has().ConnectorType, has.Has().OtherAssertCondition);
    }
}