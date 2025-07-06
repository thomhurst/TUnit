namespace TUnit.Core;

/// <summary>
/// Provides empty test data for parameterless tests.
/// </summary>
public class EmptyDataProvider : IDataProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<object?[]>> GetData()
    {
        var result = new[] { Array.Empty<object?>() };
        return Task.FromResult<IEnumerable<object?[]>>(result);
    }
}
