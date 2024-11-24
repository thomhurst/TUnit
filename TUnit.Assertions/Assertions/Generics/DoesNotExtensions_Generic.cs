using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Collections.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Generics;

public static partial class DoesNotExtensions
{
    public static InvokableValueAssertionBuilder<TActual> DoesNotContain<TActual, TInner>(this IValueSource<TActual> valueSource, TInner expected, IEqualityComparer<TInner?>? equalityComparer = null, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
        where TActual : IEnumerable<TInner>
    {
        return valueSource.RegisterAssertion(new EnumerableNotContainsExpectedValueAssertCondition<TActual, TInner>(expected, equalityComparer)
            , [doNotPopulateThisValue]);
    }
}