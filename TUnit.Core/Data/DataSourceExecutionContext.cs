using System.Threading;

namespace TUnit.Core.Data;

/// <summary>
/// Provides ambient context for data source execution, allowing the framework
/// to inject dependency tracking without modifying all the intermediate APIs.
/// </summary>
internal static class DataSourceExecutionContext
{
    private static readonly AsyncLocal<DataSourceContext?> _current = new();
    
    /// <summary>
    /// Gets the current data source context for this execution flow.
    /// </summary>
    public static DataSourceContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
    
    /// <summary>
    /// Executes an action with a specific data source context.
    /// </summary>
    public static void RunWithContext(DataSourceContext? context, Action action)
    {
        var previous = Current;
        try
        {
            Current = context;
            action();
        }
        finally
        {
            Current = previous;
        }
    }
    
    /// <summary>
    /// Executes an async function with a specific data source context.
    /// </summary>
    public static async Task<T> RunWithContextAsync<T>(DataSourceContext? context, Func<Task<T>> func)
    {
        var previous = Current;
        try
        {
            Current = context;
            return await func().ConfigureAwait(false);
        }
        finally
        {
            Current = previous;
        }
    }
    
    /// <summary>
    /// Executes an async action with a specific data source context.
    /// </summary>
    public static async Task RunWithContextAsync(DataSourceContext? context, Func<Task> func)
    {
        var previous = Current;
        try
        {
            Current = context;
            await func().ConfigureAwait(false);
        }
        finally
        {
            Current = previous;
        }
    }
}