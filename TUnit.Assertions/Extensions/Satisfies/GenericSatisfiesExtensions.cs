using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class GenericSatisfiesExtensions
{
    public static InvokableValueAssertionBuilder<TActual> Satisfies<TActual, TExpected>(
        this IValueSource<TActual> valueSource, Func<TActual, TExpected> mapper,
        Func<IValueSource<TExpected?>, InvokableAssertionBuilder<TExpected?>> assert,
        [CallerArgumentExpression("mapper")] string mapperExpression = "", 
        [CallerArgumentExpression("assert")] string assertionBuilderExpression = "")
    {
        return valueSource.RegisterAssertion(new SatisfiesAssertCondition<TActual, TExpected>(t => Task.FromResult(mapper(t)), assert, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }

    public static InvokableValueAssertionBuilder<TActual> Satisfies<TActual, TExpected>(
        this IValueSource<TActual> valueSource, Func<TActual, Task<TExpected>?> asyncMapper,
        Func<IValueSource<TExpected?>, InvokableAssertionBuilder<TExpected?>> assert,
        [CallerArgumentExpression("asyncMapper")] string mapperExpression = "", 
        [CallerArgumentExpression("assert")] string assertionBuilderExpression = "")
    {
        return valueSource.RegisterAssertion(new SatisfiesAssertCondition<TActual, TExpected>(asyncMapper, assert, assertionBuilderExpression),
            [mapperExpression, assertionBuilderExpression]);
    }
}