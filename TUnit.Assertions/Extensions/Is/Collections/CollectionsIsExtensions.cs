#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions.Collections;

public static class CollectionsIsExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsEquivalentTo<TActual, TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
    {
        return valueSource.RegisterAssertion(new EnumerableEquivalentToAssertCondition<TActual, TInner>(expected, equalityComparer)
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsEmpty<TActual>(this IValueSource<TActual> valueSource)
        where TActual : IEnumerable
    {
        return valueSource.RegisterAssertion(new EnumerableCountEqualToAssertCondition<TActual>(0)
            , []);
    }
}