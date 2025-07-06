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

/// <summary>
/// Async delegate-based test data source that stores the async factory delegate directly
/// </summary>
public sealed class AsyncDelegateDataSource : TestDataSource
{
    private readonly Func<CancellationToken, IAsyncEnumerable<object?[]>> _asyncFactory;
    private readonly bool _isShared;
    private IEnumerable<Func<object?[]>>? _cachedFactories;

    public override bool IsShared => _isShared;

    public AsyncDelegateDataSource(Func<CancellationToken, IAsyncEnumerable<object?[]>> asyncFactory, bool isShared = false)
    {
        _asyncFactory = asyncFactory ?? throw new ArgumentNullException(nameof(asyncFactory));
        _isShared = isShared;
    }

    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        if (_isShared && _cachedFactories != null)
        {
            return _cachedFactories;
        }

        var dataArrays = ConvertToSync(_asyncFactory).ToArray();
        var factories = dataArrays.Select(args => new Func<object?[]>(() => CloneArguments(args))).ToList();

        if (_isShared)
        {
            _cachedFactories = factories;
        }

        return factories;
    }

    private static IEnumerable<object?[]> ConvertToSync(Func<CancellationToken, IAsyncEnumerable<object?[]>> asyncFactory)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        var asyncEnumerable = asyncFactory(cts.Token);
        var enumerator = asyncEnumerable.GetAsyncEnumerator(cts.Token);

        try
        {
            while (true)
            {
                var moveNextTask = enumerator.MoveNextAsync().AsTask();
                try
                {
                    moveNextTask.Wait(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException("Data source generation timed out");
                }

                if (!moveNextTask.Result)
                {
                    break;
                }

                yield return enumerator.Current;
            }
        }
        finally
        {
            var disposeTask = enumerator.DisposeAsync().AsTask();
            disposeTask.Wait(TimeSpan.FromSeconds(30));
        }
    }
}

/// <summary>
/// Task-based delegate data source for methods that return Task<IEnumerable<T>>
/// </summary>
public sealed class TaskDelegateDataSource : TestDataSource
{
    private readonly Func<Task<IEnumerable<object?[]>>> _taskFactory;
    private readonly bool _isShared;
    private IEnumerable<Func<object?[]>>? _cachedFactories;

    public override bool IsShared => _isShared;

    public TaskDelegateDataSource(Func<Task<IEnumerable<object?[]>>> taskFactory, bool isShared = false)
    {
        _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
        _isShared = isShared;
    }

    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        if (_isShared && _cachedFactories != null)
        {
            return _cachedFactories;
        }

        var task = _taskFactory();
        task.Wait(TimeSpan.FromMinutes(5));

        var dataArrays = task.Result.ToArray();
        var factories = dataArrays.Select(args => new Func<object?[]>(() => CloneArguments(args))).ToList();

        if (_isShared)
        {
            _cachedFactories = factories;
        }

        return factories;
    }
}

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
