namespace TUnit.Core;

/// <summary>
/// Provides empty test data for parameterless tests.
/// </summary>
public class EmptyDataProvider : IDataProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<object?[]>> GetData()
    {
        // Return a single item array where the item is an empty array
        var result = new[] { Array.Empty<object?>() };
        return Task.FromResult<IEnumerable<object?[]>>(result);
    }
}
