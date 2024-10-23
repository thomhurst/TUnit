#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static InvokableValueAssertionBuilder<TActual> Contains<TActual, TInner>(this IValueSource<TActual> valueSource, TInner expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
    {
        return valueSource.RegisterAssertion(new EnumerableContainsExpectedValueAssertCondition<TActual, TInner>(expected, equalityComparer)
            , [doNotPopulateThisValue]);
    }
}