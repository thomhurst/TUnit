#nullable disable

using System.Collections;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static BaseAssertCondition<TActual, TAnd, TOr> SingleItem<TActual, TAnd, TOr>(this Has<TActual, TAnd, TOr> has) 
        where TActual : IEnumerable
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return has.Wrap(new EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr>(
            has.AssertionBuilder.AppendCallerMethod(null),
            0)
        );
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> DistinctItems<TActual, TAnd, TOr>(this Has<TActual, TAnd, TOr> has) 
        where TActual : IEnumerable
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return has.Wrap(new EnumerableDistinctItemsAssertCondition<TActual, object, TAnd, TOr>(
            has.AssertionBuilder.AppendCallerMethod(null),
            default,
            null)
        );
    }
    
    public static BaseAssertCondition<TActual, TAnd, TOr> DistinctItems<TActual, TInner, TAnd, TOr>(this Has<TActual, TAnd, TOr> has, IEqualityComparer<TInner> equalityComparer) 
        where TActual : IEnumerable<TInner>
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return has.Wrap(new EnumerableDistinctItemsAssertCondition<TActual, TInner, TAnd, TOr>(
            has.AssertionBuilder.AppendCallerMethod(null),
            default,
            equalityComparer)
        );
    }
    
    public static EnumerableCount<TActual, TAnd, TOr> Count<TActual, TAnd, TOr>(this Has<TActual, TAnd, TOr> has) 
        where TActual : IEnumerable
        where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
        where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
    {
        return new EnumerableCount<TActual, TAnd, TOr>(has.AssertionBuilder.AppendCallerMethod(null), has.ConnectorType, has.OtherAssertCondition);
    }
}