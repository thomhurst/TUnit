namespace TUnit.Engine.Discovery;

/// Placeholder for async data sources during test discovery - actual data resolved at execution time
internal sealed class AsyncDataSourcePlaceholder
{
    public Func<Task<object?[]?>>? ArrayFactory { get; init; }
    public Func<Task<object?>>? SingleValueFactory { get; init; }
    public Type? ExpectedType { get; init; }
    
    public object?[] GetPlaceholderArguments()
    {
        // For typed async data sources, we can sometimes infer the expected structure
        if (SingleValueFactory != null)
        {
            // Single value - show as one parameter
            return [$"<async:{ExpectedType?.Name ?? "data"}>"];
        }
        
        if (ArrayFactory != null)
        {
            // Array of values - we don't know the count yet
            return ["<async:data>"];
        }
        
        return ["<async>"];
    }
    
    public async Task<object?[]> ResolveAsync()
    {
        if (SingleValueFactory != null)
        {
            var result = await SingleValueFactory().ConfigureAwait(false);
            return [result];
        }
        
        if (ArrayFactory != null)
        {
            var result = await ArrayFactory().ConfigureAwait(false);
            return result ?? [
            ];
        }
        
        return [
        ];
    }
}