using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> DoesNotContain<TActual, TInner, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TInner expected, IEqualityComparer<TInner?>? equalityComparer = null, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return new EnumerableNotContainsAssertCondition<TActual, TInner, TAnd, TOr>(expected, equalityComparer)
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]);
    }
}