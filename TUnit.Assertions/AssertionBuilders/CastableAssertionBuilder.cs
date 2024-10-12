using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders;

public class CastableAssertionBuilder<TActual, TExpected> : InvokableValueAssertionBuilder<TActual>
{
    internal CastableAssertionBuilder(InvokableAssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder)
    {
    }
    
    public new TaskAwaiter<TExpected> GetAwaiter()
    {
        return AssertType().GetAwaiter();
    }

    private async Task<TExpected> AssertType()
    {
        var data = await ProcessAssertionsAsync();
        return (TExpected) Convert.ChangeType(data.Result, typeof(TExpected))!;
    }
}