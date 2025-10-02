using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NotNullAssertionBuilderWrapper<TActual> : InvokableValueAssertion<TActual> where TActual : class
{
    internal NotNullAssertionBuilderWrapper(InvokableAssertion<TActual?> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public new TaskAwaiter<TActual> GetAwaiter()
    {
        return Process().GetAwaiter();
    }

    private async Task<TActual> Process()
    {
        var data = await ProcessAssertionsAsync();

        var tActual = data.Result as TActual;

        return tActual!;
    }
}

public class NotNullStructAssertionBuilderWrapper<TActual> : InvokableValueAssertion<TActual> where TActual : struct
{
    internal NotNullStructAssertionBuilderWrapper(InvokableAssertion<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public new TaskAwaiter<TActual> GetAwaiter()
    {
        return Process().GetAwaiter();
    }

    private async Task<TActual> Process()
    {
        var data = await ProcessAssertionsAsync();

        var tActual = data.Result is TActual actual ? actual : default(TActual);

        return tActual;
    }
}
