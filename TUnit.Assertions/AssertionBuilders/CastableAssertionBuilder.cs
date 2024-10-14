using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders;

public class CastableAssertionBuilder<TActual, TExpected> : InvokableValueAssertionBuilder<TActual>
{
    private readonly Func<AssertionData<TActual>, TExpected?> _mapper;

    internal CastableAssertionBuilder(InvokableAssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder)
    {
        _mapper = DefaultMapper;
    }

    internal CastableAssertionBuilder(InvokableAssertionBuilder<TActual> assertionBuilder, Func<AssertionData<TActual>, TExpected?> mapper) : base(assertionBuilder)
    {
        _mapper = mapper;
    }

    public new TaskAwaiter<TExpected?> GetAwaiter()
    {
        return AssertType().GetAwaiter();
    }

    private static TExpected? DefaultMapper(AssertionData<TActual> data)
    {
        try
        {
            return (TExpected)Convert.ChangeType(data.Result, typeof(TExpected))!;
        }
        catch
        {
            return default;
        }
    }

    private async Task<TExpected?> AssertType()
    {
        var data = await ProcessAssertionsAsync();
        return _mapper(data);
    }
}