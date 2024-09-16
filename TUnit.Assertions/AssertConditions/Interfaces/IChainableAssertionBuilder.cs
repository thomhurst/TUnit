using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IChainableAssertionBuilder<out TSelf, TInnerAssertionBuilder, TActual, TAnd, TOr>
    where TSelf : IChainableAssertionBuilder<TSelf, TInnerAssertionBuilder, TActual, TAnd, TOr>
    where TInnerAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    TaskAwaiter GetAwaiter();

    internal TAssertionBuilder WithAssertion<TAssertionBuilder>(BaseAssertCondition<TActual> assertCondition)
        where TAssertionBuilder : IChainableAssertionBuilder<TAssertionBuilder, AssertionBuilder<TActual, TAnd, TOr>,
            TActual, TAnd, TOr>;

    internal AssertionBuilder<TActual, TAnd, TOr> AppendExpression(string expression);

    internal AssertionBuilder<TActual, TAnd, TOr> AppendConnector(ChainType chainType);

    internal AssertionBuilder<TActual, TAnd, TOr> AppendCallerMethod(string? expression,
        [CallerMemberName] string methodName = "");

    internal AssertionBuilder<TActual, TAnd, TOr> AppendCallerMethodWithMultipleExpressions(string?[] expressions,
        [CallerMemberName] string methodName = "");
}