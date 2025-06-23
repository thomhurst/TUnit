namespace TUnit.Core;

/// <summary>
/// Provides test data from an ArgumentsAttribute.
/// </summary>
public class ArgumentsDataProvider : IDataProvider
{
    private readonly ArgumentsAttribute _argumentsAttribute;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentsDataProvider"/> class.
    /// </summary>
    /// <param name="argumentsAttribute">The arguments attribute containing test data.</param>
    public ArgumentsDataProvider(ArgumentsAttribute argumentsAttribute)
    {
        _argumentsAttribute = argumentsAttribute ?? throw new ArgumentNullException(nameof(argumentsAttribute));
    }

    /// <inheritdoc />
    public Task<IEnumerable<object?[]>> GetData()
    {
        // ArgumentsAttribute.Values is already an object?[] array,
        // so we wrap it in a single-element collection
        var result = new[] { _argumentsAttribute.Values };
        return Task.FromResult<IEnumerable<object?[]>>(result);
    }
}