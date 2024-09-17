#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotEquivalentTo<TActual, TInner, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, IEnumerable<TInner> expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableNotEquivalentToAssertCondition<TActual, TInner, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, equalityComparer)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNotEmpty<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EnumerableCountNotEqualToAssertCondition<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(null), 0)
            .ChainedTo(assertionBuilder);
    }
}