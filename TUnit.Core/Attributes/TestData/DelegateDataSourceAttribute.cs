namespace TUnit.Core;

/// <summary>
/// Internal data source attribute for delegate-based data generation
/// Used to replace DelegateDataSource
/// </summary>
internal sealed class DelegateDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private readonly Func<DataGeneratorMetadata, IAsyncEnumerable<object?[]>> _factory;
    private readonly bool _isShared;
    private List<Func<Task<object?[]?>>>? _cachedFactories;
    
    public DelegateDataSourceAttribute(Func<DataGeneratorMetadata, IAsyncEnumerable<object?[]>> factory, bool isShared = false)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _isShared = isShared;
    }
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (_isShared && _cachedFactories != null)
        {
            foreach (var factory in _cachedFactories)
            {
                yield return factory;
            }
            yield break;
        }

        var factories = new List<Func<Task<object?[]?>>>();
        
        await foreach (var data in _factory(dataGeneratorMetadata))
        {
            var clonedData = CloneArguments(data);
            var factory = new Func<Task<object?[]?>>(() => Task.FromResult<object?[]?>(clonedData));
            factories.Add(factory);
            yield return factory;
        }

        if (_isShared)
        {
            _cachedFactories = factories;
        }
    }
    
    private static object?[] CloneArguments(object?[] args)
    {
        var cloned = new object?[args.Length];
        Array.Copy(args, cloned, args.Length);
        return cloned;
    }
}