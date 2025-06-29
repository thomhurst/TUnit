namespace TUnit.Core;

/// <summary>
/// Provides test data from method delegates.
/// </summary>
public class MethodDataProvider : IDataProvider
{
    private readonly Func<Task<IEnumerable<object?[]>>> _dataFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodDataProvider"/> class with a single value provider.
    /// </summary>
    /// <param name="valueProvider">A function that provides a single test value.</param>
    public MethodDataProvider(Func<object?> valueProvider)
    {
        _dataFactory = () => Task.FromResult<IEnumerable<object?[]>>(new[] { new[] { valueProvider() } });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodDataProvider"/> class with an async single value provider.
    /// </summary>
    /// <param name="valueProvider">An async function that provides a single test value.</param>
    public MethodDataProvider(Func<Task<object?>> valueProvider)
    {
        _dataFactory = async () =>
        {
            var value = await valueProvider();
            return new[] { new[] { value } };
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodDataProvider"/> class with a multiple values provider.
    /// </summary>
    /// <param name="valuesProvider">A function that provides multiple test values.</param>
    public MethodDataProvider(Func<IEnumerable<object?>> valuesProvider)
    {
        _dataFactory = () =>
        {
            var values = valuesProvider();
            var result = values.Select(value => new[] { value }).ToArray();
            return Task.FromResult<IEnumerable<object?[]>>(result);
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodDataProvider"/> class with an async multiple values provider.
    /// </summary>
    /// <param name="valuesProvider">An async function that provides multiple test values.</param>
    public MethodDataProvider(Func<Task<IEnumerable<object?>>> valuesProvider)
    {
        _dataFactory = async () =>
        {
            var values = await valuesProvider();
            return values.Select(value => new[] { value }).ToArray();
        };
    }

    /// <inheritdoc />
    public Task<IEnumerable<object?[]>> GetData()
    {
        return _dataFactory();
    }
}
