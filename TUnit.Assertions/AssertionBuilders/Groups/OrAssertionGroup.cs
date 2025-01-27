﻿using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders.Groups;

public class OrAssertionGroup<TActual, TAssertionBuilder> : AssertionGroup<TActual, TAssertionBuilder>
    where TAssertionBuilder : AssertionBuilder
{
    private readonly Stack<BaseAssertCondition> _assertConditions = [];
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
        ((ISource)AssertionBuilder).Assertions.Clear();
        
        foreach (var condition in _assertConditions)
        {
            ((ISource)AssertionBuilder).Assertions.Push(condition);
        }
        
        return (TActual?) await _invokableAssertionBuilder!.ProcessAssertionsAsync(x => x.Result);
    }

    private void Push(TAssertionBuilder assertionBuilder, Func<TAssertionBuilder, InvokableAssertionBuilder<TActual>> assert)
    {
        if (_assertConditions.Count > 0)
        {
            _assertConditions.Push(new OrAssertCondition(_assertConditions.Pop(), assert(assertionBuilder).Assertions.Pop()));
        }
        else
        {
            var invokableAssertionBuilder = assert(assertionBuilder);
            assertionBuilder.AppendConnector(ChainType.Or);
            _invokableAssertionBuilder = invokableAssertionBuilder;
            _assertConditions.Push(_invokableAssertionBuilder.Assertions.Pop());
        }
    }
}