﻿using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public class MappableResultAssertionBuilder<TActual, TExpected> : InvokableValueAssertionBuilder<TActual>
{
    private readonly Func<TActual?, TExpected?> _mapper;

    internal MappableResultAssertionBuilder(InvokableAssertionBuilder<TActual> assertionBuilder, Func<TActual?, TExpected?> mapper) : base(assertionBuilder)
    {
        _mapper = mapper;
    }

    public new TaskAwaiter<TExpected?> GetAwaiter()
    {
        return Map().GetAwaiter();
    }

    private async Task<TExpected?> Map()
    {
        var data = await ProcessAssertionsAsync();

        var tActual = data.Result is TActual actual ? actual : default(TActual?);

        return _mapper(tActual);
    }
}

public class MappableResultAssertionBuilder<TActual, TAssertCondition, TExpected> : InvokableValueAssertionBuilder<TActual>
    where TAssertCondition : BaseAssertCondition<TActual>
{
    private readonly TAssertCondition _assertCondition;
    private readonly Func<TActual?, TAssertCondition, TExpected?> _mapper;

    internal MappableResultAssertionBuilder(InvokableAssertionBuilder<TActual> assertionBuilder, TAssertCondition assertCondition, Func<TActual?, TAssertCondition, TExpected?> mapper) : base(assertionBuilder)
    {
        _assertCondition = assertCondition;
        _mapper = mapper;
    }

    public new TaskAwaiter<TExpected?> GetAwaiter()
    {
        return Map().GetAwaiter();
    }

    private async Task<TExpected?> Map()
    {
        var data = await ProcessAssertionsAsync();

        var tActual = data.Result is TActual actual ? actual : default(TActual?);

        return _mapper(tActual, _assertCondition);
    }
}
