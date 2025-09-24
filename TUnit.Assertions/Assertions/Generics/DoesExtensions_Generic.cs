#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static AssertionBuilder<TActual> Contains<TActual, TInner>(this IValueSource<TActual> valueSource, TInner expected, IEqualityComparer<TInner> equalityComparer = null, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
        where TActual : IEnumerable<TInner>
    {
        return valueSource.RegisterAssertion(new EnumerableContainsExpectedValueAssertCondition<TActual, TInner>(expected, equalityComparer)
            , [doNotPopulateThisValue]);
    }

    public static ContainsAssertionBuilder<IEnumerable<TInner>, TInner> Contains<TInner>(this IValueSource<IEnumerable<TInner>> valueSource, Func<TInner, bool> matcher, [CallerArgumentExpression(nameof(matcher))] string doNotPopulateThisValue = null)
    {
        var containsCondition = new EnumerableContainsExpectedFuncAssertCondition<IEnumerable<TInner>, TInner>(matcher, doNotPopulateThisValue);
        var assertionBuilder = valueSource.RegisterAssertion(containsCondition, [doNotPopulateThisValue]);
        return new ContainsAssertionBuilder<IEnumerable<TInner>, TInner>(assertionBuilder, containsCondition);
    }

    public static AssertionBuilder<IEnumerable<TInner>> ContainsOnly<TInner>(this IValueSource<IEnumerable<TInner>> valueSource, Func<TInner, bool> matcher, [CallerArgumentExpression(nameof(matcher))] string doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new EnumerableAllExpectedFuncAssertCondition<IEnumerable<TInner>, TInner>(matcher, doNotPopulateThisValue)
            , [doNotPopulateThisValue]);
    }

}
