namespace TUnit.Core;

/// <summary>
/// Provides test data from arguments.
/// </summary>
public class ArgumentsDataProvider : IDataProvider
{
    private readonly object?[] _arguments;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentsDataProvider"/> class.
    /// </summary>
    /// <param name="arguments">The arguments for the test data.</param>
    public ArgumentsDataProvider(params object?[] arguments)
    {
        _arguments = arguments ?? Array.Empty<object?>();
    }

    /// <inheritdoc />
    public Task<IEnumerable<object?[]>> GetData()
    {
        // Wrap the arguments array in a single-element collection
        var result = new[] { _arguments };
        return Task.FromResult<IEnumerable<object?[]>>(result);
    }
}