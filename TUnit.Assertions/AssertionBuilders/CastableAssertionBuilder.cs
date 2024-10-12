using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders;

public class CastableAssertionBuilder<TActual, TExpected> : InvokableValueAssertionBuilder<TActual>
{
    private readonly Func<TActual?, TExpected?> _mapper;

    internal CastableAssertionBuilder(InvokableAssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder)
    {
        _mapper = DefaultMapper;
    }

    internal CastableAssertionBuilder(InvokableAssertionBuilder<TActual> assertionBuilder, Func<TActual?, TExpected?> mapper) : base(assertionBuilder)
    {
        _mapper = mapper;
    }

    public new TaskAwaiter<TExpected> GetAwaiter()
    {
        return AssertType().GetAwaiter();
    }

    private static TExpected? DefaultMapper(TActual? data)
    {
        return (TExpected)Convert.ChangeType(data, typeof(TExpected));
    }

    private async Task<TExpected?> AssertType()
    {
        var data = await ProcessAssertionsAsync();
        return _mapper(data.Result);
    }
}