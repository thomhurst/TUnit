using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public class CollectionWrapper<TActual, TInner>(IValueSource<TActual> valueSource) where TActual : IEnumerable<TInner>
{
    public InvokableValueAssertionBuilder<TActual> Satisfy<TExpected>(
        Func<TInner?, TExpected> mapper,
        Func<IValueSource<TExpected?>, InvokableAssertionBuilder<TExpected?>> assert,
        [CallerArgumentExpression(nameof(mapper))] string mapperExpression = "", 
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        var subject = "items mapped by " + mapperExpression;
        return valueSource.RegisterAssertion(new EnumerableSatisfiesAssertCondition<TActual, TInner, TExpected>(t => Task.FromResult(mapper(t)), assert, subject, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }

    
    public InvokableValueAssertionBuilder<TActual> Satisfy<TExpected>(
        Func<TInner?, Task<TExpected>?> asyncMapper,
        Func<IValueSource<TExpected?>, InvokableAssertionBuilder<TExpected?>> assert,
        [CallerArgumentExpression(nameof(asyncMapper))] string mapperExpression = "", 
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        var subject = "items mapped by " + mapperExpression;
        return valueSource.RegisterAssertion(new EnumerableSatisfiesAssertCondition<TActual, TInner, TExpected>(asyncMapper, assert, subject, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }
    
    public InvokableValueAssertionBuilder<TActual> Satisfy(
        Func<IValueSource<TInner?>, InvokableAssertionBuilder<TInner?>> assert,
        [CallerArgumentExpression(nameof(assert))] string assertionBuilderExpression = "")
    {
        return valueSource.RegisterAssertion(new EnumerableSatisfiesAssertCondition<TActual, TInner, TInner>(
                inner => inner == null ? null : Task.FromResult(inner), assert, "items", assertionBuilderExpression),
            ["", assertionBuilderExpression]);
    }
}