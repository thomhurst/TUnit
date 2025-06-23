namespace TUnit.Core;

/// <summary>
/// Provides test data from a single set of arguments.
/// </summary>
public class SingleArgumentsDataProvider : IDataProvider
{
    private readonly object?[] _arguments;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleArgumentsDataProvider"/> class.
    /// </summary>
    /// <param name="arguments">The arguments to provide.</param>
    public SingleArgumentsDataProvider(object?[] arguments)
    {
        _arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
    }

    /// <inheritdoc />
    public Task<IEnumerable<object?[]>> GetData()
    {
        // Wrap the arguments in a single-element collection
        var result = new[] { _arguments };
        return Task.FromResult<IEnumerable<object?[]>>(result);
    }
}