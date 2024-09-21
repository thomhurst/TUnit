#nullable disable

using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsNotExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsNotEquivalentTo<TActual, TInner>(this IValueSource<TActual> valueSource, IEnumerable<TInner> expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
    {
        return valueSource.RegisterAssertion(new EnumerableNotEquivalentToAssertCondition<TActual, TInner>(expected, equalityComparer)
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotEmpty<TActual>(this IValueSource<TActual> valueSource)
        where TActual : IEnumerable
    {
        return valueSource.RegisterAssertion(new EnumerableCountNotEqualToAssertCondition<TActual>(0)
            , []);
    }
}