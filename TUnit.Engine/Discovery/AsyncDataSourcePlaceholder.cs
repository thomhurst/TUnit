using System;
using System.Threading.Tasks;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Placeholder for async data sources during test discovery.
/// Actual data will be resolved during test execution.
/// </summary>
internal sealed class AsyncDataSourcePlaceholder
{
    public Func<Task<object?[]?>>? ArrayFactory { get; init; }
    public Func<Task<object?>>? SingleValueFactory { get; init; }
    public Type? ExpectedType { get; init; }
    
    /// <summary>
    /// Creates placeholder arguments for display during discovery
    /// </summary>
    public object?[] GetPlaceholderArguments()
    {
        // For typed async data sources, we can sometimes infer the expected structure
        if (SingleValueFactory != null)
        {
            // Single value - show as one parameter
            return new object?[] { $"<async:{ExpectedType?.Name ?? "data"}>" };
        }
        
        if (ArrayFactory != null)
        {
            // Array of values - we don't know the count yet
            return new object?[] { "<async:data>" };
        }
        
        return new object?[] { "<async>" };
    }
    
    /// <summary>
    /// Resolves the actual data by executing the async factory
    /// </summary>
    public async Task<object?[]> ResolveAsync()
    {
        if (SingleValueFactory != null)
        {
            var result = await SingleValueFactory().ConfigureAwait(false);
            return new[] { result };
        }
        
        if (ArrayFactory != null)
        {
            var result = await ArrayFactory().ConfigureAwait(false);
            return result ?? Array.Empty<object?>();
        }
        
        return Array.Empty<object?>();
    }
}