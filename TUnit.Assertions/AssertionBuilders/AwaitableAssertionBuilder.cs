using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// An assertion builder that can be awaited to return a value after assertions pass
/// </summary>
public class AwaitableAssertionBuilder<TActual> : AssertionBuilder<TActual>
{
    public AwaitableAssertionBuilder(TActual value, string? actualExpression) 
        : base(value, actualExpression)
    {
    }

    public AwaitableAssertionBuilder(Func<TActual> valueFunc, string? actualExpression) 
        : base(valueFunc, actualExpression)
    {
    }

    public AwaitableAssertionBuilder(Func<Task<TActual>> asyncFunc, string? actualExpression) 
        : base(asyncFunc, actualExpression)
    {
    }

    public AwaitableAssertionBuilder(Task<TActual> task, string? actualExpression) 
        : base(task, actualExpression)
    {
    }

    public AwaitableAssertionBuilder(ValueTask<TActual> valueTask, string? actualExpression) 
        : base(valueTask, actualExpression)
    {
    }

    // Custom awaiter that returns TActual
    public new TaskAwaiter<TActual> GetAwaiter() => GetResultAsync().GetAwaiter();

    private async Task<TActual> GetResultAsync()
    {
        await ProcessAssertionsAsync();
        var data = await GetAssertionData();
        return (TActual)data.Result!;
    }
}