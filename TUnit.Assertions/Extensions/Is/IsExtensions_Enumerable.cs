#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsEquivalentTo<TActual, TInner, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, IEnumerable<TInner> expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableEquivalentToAssertCondition<TActual, TInner, TAnd, TOr>(expected, equalityComparer)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsEmpty<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource)
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr>(0)
            .ChainedTo(valueSource.AssertionBuilder, []);
    }
}