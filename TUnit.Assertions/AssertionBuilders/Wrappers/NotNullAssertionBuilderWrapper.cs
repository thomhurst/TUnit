using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NotNullAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual>
{
    internal NotNullAssertionBuilderWrapper(InvokableAssertionBuilder<TActual?> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }
    
    public new TaskAwaiter<TActual> GetAwaiter()
    {
        return Process().GetAwaiter();
    }
    
    private async Task<TActual> Process()
    {
        var data = await ProcessAssertionsAsync();

        var tActual = data.Result is TActual actual ? actual : default;

        return tActual!;
    }
}