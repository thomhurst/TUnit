using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public class CollectionWrapper<TInner>(IValueSource<IEnumerable<TInner>> valueSource)
{
    public InvokableValueAssertion<IEnumerable<TInner>> Satisfy<TExpected>(
        Func<TInner?, TExpected> mapper,
        Func<IValueSource<TExpected?>, IInvokableAssertion> assert,
        [CallerArgumentExpression(nameof(mapper))] string mapperExpression = "",
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        var subject = "items mapped by " + mapperExpression;
        return valueSource.RegisterAssertion(new EnumerableSatisfiesAssertCondition<IEnumerable<TInner>, TInner, TExpected>(t => Task.FromResult(mapper(t)), assert, subject, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }


    public InvokableValueAssertion<IEnumerable<TInner>> Satisfy<TExpected>(
        Func<TInner?, Task<TExpected>?> asyncMapper,
        Func<IValueSource<TExpected?>, IInvokableAssertion> assert,
        [CallerArgumentExpression(nameof(asyncMapper))] string mapperExpression = "",
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        var subject = "items mapped by " + mapperExpression;
        return valueSource.RegisterAssertion(new EnumerableSatisfiesAssertCondition<IEnumerable<TInner>, TInner, TExpected>(asyncMapper, assert, subject, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }

    public InvokableValueAssertion<IEnumerable<TInner>> Satisfy(
        Func<IValueSource<TInner?>, IInvokableAssertion> assert,
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        return valueSource.RegisterAssertion(new EnumerableSatisfiesAssertCondition<IEnumerable<TInner>, TInner, TInner>(
                inner => inner == null ? null : Task.FromResult(inner), assert, "items", assertionBuilderExpression),
            ["", assertionBuilderExpression]);
    }
}
