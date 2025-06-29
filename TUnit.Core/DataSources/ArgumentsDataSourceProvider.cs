using TUnit.Core.Interfaces;

namespace TUnit.Core.DataSources;

/// <summary>
/// Data source provider for static arguments (e.g., from [Arguments] attribute)
/// This is AOT-compatible as it only deals with compile-time known values
/// </summary>
public sealed class ArgumentsDataSourceProvider : IAotCompatibleDataSource
{
    private readonly object?[][] _data;
    
    public ArgumentsDataSourceProvider(params object?[][] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }
    
    public ArgumentsDataSourceProvider(object?[] singleSet)
    {
        _data = new[] { singleSet ?? throw new ArgumentNullException(nameof(singleSet)) };
    }

    public IEnumerable<Func<object?[]>> GenerateDataFactories(DataSourceContext context)
    {
        // For static data, create factories that return cloned arrays for isolation
        return _data.Select(args => new Func<object?[]>(() => CloneArguments(args)));
    }
    
    public bool IsShared => true; // Static data is shared but cloned for each test
    
    private static object?[] CloneArguments(object?[] args)
    {
        var cloned = new object?[args.Length];
        Array.Copy(args, cloned, args.Length);
        return cloned;
    }
}