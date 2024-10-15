﻿using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions.AssertionBuilders.Groups;

public class OrAssertionGroup<TActual, TAssertionBuilder> : AssertionGroup<TActual, TAssertionBuilder>
    where TAssertionBuilder : AssertionBuilder<TActual>
{
    private readonly Stack<BaseAssertCondition<TActual>> _assertConditions = [];
    private InvokableAssertionBuilder<TActual>? _invokableAssertionBuilder;

    internal OrAssertionGroup(Func<TAssertionBuilder, InvokableAssertionBuilder<TActual>> initialAssert, Func<TAssertionBuilder, InvokableAssertionBuilder<TActual>> assert, TAssertionBuilder assertionBuilder) : base(assertionBuilder)
    {
        Push(assertionBuilder, initialAssert);
        Push(assertionBuilder, assert);
    }

    public OrAssertionGroup<TActual, TAssertionBuilder> Or(Func<TAssertionBuilder, InvokableAssertionBuilder<TActual>> assert)
    {
        Push(AssertionBuilder, assert);
        return this;
    }
    
    public override TaskAwaiter<TActual?> GetAwaiter()
    {
        return GetResult().GetAwaiter();
    }

    private async Task<TActual?> GetResult()
    {
        foreach (var condition in _assertConditions)
        {
            AssertionBuilder.Assertions.Push(condition);
        }
        
        return await _invokableAssertionBuilder!.ProcessAssertionsAsync(x => x.Result);
    }

    private void Push(TAssertionBuilder assertionBuilder, Func<TAssertionBuilder, InvokableAssertionBuilder<TActual>> assert)
    {
        InvokableAssertionBuilder<TActual> invokableAssertionBuilder;
        
        if (_assertConditions.TryPop(out var assertCondition))
        {
            invokableAssertionBuilder = assert(assertionBuilder);
            var assertion2 = invokableAssertionBuilder.Assertions.Pop();
            _assertConditions.Push(new OrAssertCondition<TActual>(assertCondition, assertion2));
        }
        else
        {
            invokableAssertionBuilder = assert(assertionBuilder);
            assertionBuilder.AppendConnector(ChainType.Or);
            _invokableAssertionBuilder = invokableAssertionBuilder;
            _assertConditions.Push(_invokableAssertionBuilder.Assertions.Pop());
        }
    }
}