namespace TUnit.Assertions;

public class AsyncDelegateAssertionBuilder<T> : AssertionBuilder<T>
{
    private readonly Func<Task<T?>> _function;

    internal AsyncDelegateAssertionBuilder(Func<Task<T?>> function)
    {
        _function = function;
    }

    protected internal override async Task<AssertionData<T>> GetAssertionData()
    {
        var assertionData = await _function.InvokeAndGetExceptionAsync();
        
        return assertionData!;
    }
}

public class AsyncDelegateAssertionBuilder : AssertionBuilder<object>
{
    private readonly Func<Task> _function;

    internal AsyncDelegateAssertionBuilder(Func<Task> function)
    {
        _function = function;
    }

    protected internal override async Task<AssertionData<object>> GetAssertionData()
    {
        var exception = await _function.InvokeAndGetExceptionAsync();
        
        return new AssertionData<object>(null, exception);
    }
}