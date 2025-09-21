using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NotNullDelegateAssertionBuilderWrapper<TResult> : InvokableValueAssertionBuilder<Func<TResult>>
{
    internal NotNullDelegateAssertionBuilderWrapper(InvokableAssertionBuilder<Func<TResult>?> invokableAssertionBuilder) 
        : base(invokableAssertionBuilder)
    {
    }

    public new TaskAwaiter<Func<TResult>> GetAwaiter()
    {
        return Process().GetAwaiter();
    }

    private async Task<Func<TResult>> Process()
    {
        var data = await ProcessAssertionsAsync();
        
        if (data.Result is Func<TResult> func)
        {
            return func;
        }

        throw new InvalidOperationException("Expected non-null Func<TResult>");
    }
}

public class NotNullActionAssertionBuilderWrapper : InvokableValueAssertionBuilder<Action>
{
    internal NotNullActionAssertionBuilderWrapper(InvokableAssertionBuilder<Action?> invokableAssertionBuilder) 
        : base(invokableAssertionBuilder)
    {
    }

    public new TaskAwaiter<Action> GetAwaiter()
    {
        return Process().GetAwaiter();
    }

    private async Task<Action> Process()
    {
        var data = await ProcessAssertionsAsync();
        
        if (data.Result is Action action)
        {
            return action;
        }

        throw new InvalidOperationException("Expected non-null Action");
    }
}

public class NotNullAsyncDelegateAssertionBuilderWrapper<TResult> : InvokableValueAssertionBuilder<Func<Task<TResult>>>
{
    internal NotNullAsyncDelegateAssertionBuilderWrapper(InvokableAssertionBuilder<Func<Task<TResult>>?> invokableAssertionBuilder) 
        : base(invokableAssertionBuilder)
    {
    }

    public new TaskAwaiter<Func<Task<TResult>>> GetAwaiter()
    {
        return Process().GetAwaiter();
    }

    private async Task<Func<Task<TResult>>> Process()
    {
        var data = await ProcessAssertionsAsync();
        
        if (data.Result is Func<Task<TResult>> func)
        {
            return func;
        }

        throw new InvalidOperationException("Expected non-null Func<Task<TResult>>");
    }
}

public class NotNullAsyncActionAssertionBuilderWrapper : InvokableValueAssertionBuilder<Func<Task>>
{
    internal NotNullAsyncActionAssertionBuilderWrapper(InvokableAssertionBuilder<Func<Task>?> invokableAssertionBuilder) 
        : base(invokableAssertionBuilder)
    {
    }

    public new TaskAwaiter<Func<Task>> GetAwaiter()
    {
        return Process().GetAwaiter();
    }

    private async Task<Func<Task>> Process()
    {
        var data = await ProcessAssertionsAsync();
        
        if (data.Result is Func<Task> func)
        {
            return func;
        }

        throw new InvalidOperationException("Expected non-null Func<Task>");
    }
}