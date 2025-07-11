using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Base class for test data sources that provide data through factories
/// </summary>
public abstract class TestDataSource : IDataSource
{
    /// <summary>
    /// Gets factory functions that produce test data when invoked
    /// </summary>
    public abstract IEnumerable<Func<object?[]>> GetDataFactories();

    /// <summary>
    /// Indicates whether this data source is shared across tests
    /// </summary>
    public abstract bool IsShared { get; }

    /// <summary>
    /// Implementation of IDataSource interface
    /// </summary>
    public IEnumerable<Func<object?[]>> GenerateDataFactories(DataSourceContext context)
    {
        return GetDataFactories();
    }

    /// <summary>
    /// Helper method to clone arguments for test isolation
    /// </summary>
    protected static object?[] CloneArguments(object?[] args)
    {
        var cloned = new object?[args.Length];
        Array.Copy(args, cloned, args.Length);
        return cloned;
    }
}

/// <summary>
/// Static test data source for compile-time known values (e.g., from [Arguments] attribute)
/// </summary>
public sealed class StaticTestDataSource : TestDataSource
{
    private readonly object?[][] _data;

    public override bool IsShared => false;

    public StaticTestDataSource(params object?[][] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        return _data.Select(args => new Func<object?[]>(() => CloneArguments(args)));
    }
}

/// <summary>
/// Delegate-based test data source that stores the factory delegate directly
/// </summary>
public sealed class DelegateDataSource : TestDataSource
{
    private readonly Func<IEnumerable<object?[]>> _factory;
    private readonly bool _isShared;
    private IEnumerable<Func<object?[]>>? _cachedFactories;

    public override bool IsShared => _isShared;

    public DelegateDataSource(Func<IEnumerable<object?[]>> factory, bool isShared = false)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _isShared = isShared;
    }

    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        if (_isShared && _cachedFactories != null)
        {
            return _cachedFactories;
        }

        var dataArrays = _factory().ToArray();
        var factories = dataArrays.Select(args => new Func<object?[]>(() => CloneArguments(args))).ToList();

        if (_isShared)
        {
            _cachedFactories = factories;
        }

        return factories;
    }
}

// Note: AsyncDelegateDataSource has been moved to AsyncTestDataSources.cs
// It now properly implements async patterns without sync-over-async issues

// Note: TaskDelegateDataSource has been moved to AsyncTestDataSources.cs
// It now properly implements async patterns without sync-over-async issues

/// <summary>
/// Property data source that provides a single value for property injection
/// </summary>
public sealed class PropertyDataSource
{
    public required string PropertyName { get; init; }
    public required Type PropertyType { get; init; }
    public required TestDataSource DataSource { get; init; }
}

// Obsolete classes for backward compatibility
[Obsolete("Use DelegateDataSource instead")]
public sealed class DynamicTestDataSource : TestDataSource
{
    public required string FactoryKey { get; init; }
    private readonly bool _isShared;

    public override bool IsShared => _isShared;

    public DynamicTestDataSource(bool isShared = false)
    {
        _isShared = isShared;
    }

    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        throw new NotSupportedException("DynamicTestDataSource is obsolete. Use DelegateDataSource instead.");
    }
}

[Obsolete("Use AsyncDelegateDataSource instead")]
public sealed class AsyncDynamicTestDataSource : TestDataSource
{
    public required string FactoryKey { get; init; }
    private readonly bool _isShared;

    public override bool IsShared => _isShared;

    public AsyncDynamicTestDataSource(bool isShared = false)
    {
        _isShared = isShared;
    }

    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        throw new NotSupportedException("AsyncDynamicTestDataSource is obsolete. Use AsyncDelegateDataSource instead.");
    }
}
