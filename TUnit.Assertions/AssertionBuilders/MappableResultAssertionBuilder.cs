using System.Runtime.CompilerServices;

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

        var tActual = data.Result is TActual actual ? actual : default;
        
        return _mapper(tActual);
    }
}