using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public class CollectionWrapper<TInner>(IValueSource<IEnumerable<TInner>> valueSource)
{
    public AssertionBuilder<IEnumerable<TInner>> Satisfy<TExpected>(
        Func<TInner?, TExpected> mapper,
        Func<IValueSource<TExpected?>, AssertionBuilder> assert,
        [CallerArgumentExpression(nameof(mapper))] string mapperExpression = "",
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        var subject = "items mapped by " + mapperExpression;
        return valueSource.RegisterAssertion(new EnumerableSatisfiesAssertCondition<IEnumerable<TInner>, TInner, TExpected>(t => Task.FromResult(mapper(t)), assert, subject, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }


    public AssertionBuilder<IEnumerable<TInner>> Satisfy<TExpected>(
        Func<TInner?, Task<TExpected>?> asyncMapper,
        Func<IValueSource<TExpected?>, AssertionBuilder> assert,
        [CallerArgumentExpression(nameof(asyncMapper))] string mapperExpression = "",
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        var subject = "items mapped by " + mapperExpression;
        return valueSource.RegisterAssertion(new EnumerableSatisfiesAssertCondition<IEnumerable<TInner>, TInner, TExpected>(asyncMapper, assert, subject, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }

    public AssertionBuilder<IEnumerable<TInner>> Satisfy(
        Func<IValueSource<TInner?>, AssertionBuilder> assert,
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        return valueSource.RegisterAssertion(new EnumerableSatisfiesAssertCondition<IEnumerable<TInner>, TInner, TInner>(
                inner => inner == null ? null : Task.FromResult(inner), assert, "items", assertionBuilderExpression),
            ["", assertionBuilderExpression]);
    }
}
