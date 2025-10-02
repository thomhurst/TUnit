using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders.Groups;

public class OrAssertionGroup<TActual, TAssertionBuilder> : AssertionGroup<TActual, TAssertionBuilder>
    where TAssertionBuilder : AssertionCore
{
    private readonly Stack<BaseAssertCondition> _assertConditions = [];
    private InvokableAssertion<TActual>? _invokableAssertionBuilder;

    internal OrAssertionGroup(Func<TAssertionBuilder, InvokableAssertion<TActual>> initialAssert, Func<TAssertionBuilder, InvokableAssertion<TActual>> assert, TAssertionBuilder assertionBuilder) : base(assertionBuilder)
    {
        Push(assertionBuilder, initialAssert);
        Push(assertionBuilder, assert);
    }

    public OrAssertionGroup<TActual, TAssertionBuilder> Or(Func<TAssertionBuilder, InvokableAssertion<TActual>> assert)
    {
        Push(AssertionCore, assert);
        return this;
    }

    public override TaskAwaiter<TActual?> GetAwaiter()
    {
        return GetResult().GetAwaiter();
    }

    private async Task<TActual?> GetResult()
    {
        ((ISource) AssertionCore).Assertions.Clear();

        foreach (var condition in _assertConditions)
        {
            ((ISource) AssertionCore).Assertions.Push(condition);
        }

        return (TActual?) await _invokableAssertionBuilder!.ProcessAssertionsAsync(x => Task.FromResult(x.Result));
    }

    private void Push(TAssertionBuilder assertionBuilder, Func<TAssertionBuilder, InvokableAssertion<TActual>> assert)
    {
        if (_assertConditions.Count > 0)
        {
            _assertConditions.Push(new OrAssertCondition(_assertConditions.Pop(), ((ISource) assert(assertionBuilder)).Assertions.Pop()));
        }
        else
        {
            var invokableAssertionBuilder = assert(assertionBuilder);
            assertionBuilder.AppendConnector(ChainType.Or);
            _invokableAssertionBuilder = invokableAssertionBuilder;
            _assertConditions.Push(((ISource) _invokableAssertionBuilder).Assertions.Pop());
        }
    }
}
