using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Base class for async test data sources that provide data through factories
/// </summary>
public abstract class AsyncTestDataSource : TestDataSource, IAsyncDataSource
{
    /// <summary>
    /// Gets factory functions that produce test data when invoked asynchronously
    /// </summary>
    public abstract IAsyncEnumerable<Func<object?[]>> GetDataFactoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Implementation of IAsyncDataSource interface
    /// </summary>
    public IAsyncEnumerable<Func<object?[]>> GenerateDataFactoriesAsync(DataSourceContext context, CancellationToken cancellationToken = default)
    {
        return GetDataFactoriesAsync(cancellationToken);
    }

    /// <summary>
    /// Synchronous implementation for backward compatibility - converts async to sync
    /// This should be avoided where possible in favor of the async API
    /// </summary>
    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        // This is a bridge for backward compatibility
        // Consider logging a warning here in development
        return GetDataFactoriesSync();
    }

    /// <summary>
    /// Helper method to get data synchronously for backward compatibility
    /// </summary>
    protected virtual IEnumerable<Func<object?[]>> GetDataFactoriesSync()
    {
        // Default implementation that materializes the async enumerable
        // This is not ideal but provides backward compatibility
        var factories = new List<Func<object?[]>>();
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var asyncEnumerator = GetDataFactoriesAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        
        try
        {
            while (true)
            {
                // Use a more robust approach than .Wait()
                var moveNextTask = asyncEnumerator.MoveNextAsync().AsTask();
                if (!moveNextTask.IsCompleted)
                {
                    // Use spinning wait with periodic yielding to avoid thread pool starvation
                    var deadline = DateTime.UtcNow.AddMinutes(5);
                    while (!moveNextTask.IsCompleted && DateTime.UtcNow < deadline)
                    {
                        Thread.Yield();
                        Thread.Sleep(1);
                    }
                    
                    if (!moveNextTask.IsCompleted)
                    {
                        throw new TimeoutException("Data source generation timed out");
                    }
                }

                if (moveNextTask.IsFaulted)
                {
                    throw moveNextTask.Exception!.InnerException ?? moveNextTask.Exception;
                }

                if (moveNextTask.IsCanceled || !moveNextTask.Result)
                {
                    break;
                }

                factories.Add(asyncEnumerator.Current);
            }
        }
        finally
        {
            var disposeTask = asyncEnumerator.DisposeAsync().AsTask();
            if (!disposeTask.IsCompleted)
            {
                disposeTask.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        return factories;
    }
}

/// <summary>
/// Async delegate data source for methods that return IAsyncEnumerable<T>
/// </summary>
public sealed class AsyncDelegateDataSource : AsyncTestDataSource
{
    private readonly Func<CancellationToken, IAsyncEnumerable<object?[]>> _asyncFactory;
    private readonly bool _isShared;
    private List<Func<object?[]>>? _cachedFactories;

    public override bool IsShared => _isShared;

    public AsyncDelegateDataSource(Func<CancellationToken, IAsyncEnumerable<object?[]>> asyncFactory, bool isShared = false)
    {
        _asyncFactory = asyncFactory ?? throw new ArgumentNullException(nameof(asyncFactory));
        _isShared = isShared;
    }

    public override async IAsyncEnumerable<Func<object?[]>> GetDataFactoriesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_isShared && _cachedFactories != null)
        {
            foreach (var factory in _cachedFactories)
            {
                yield return factory;
            }
            yield break;
        }

        var factories = new List<Func<object?[]>>();
        
        await foreach (var data in _asyncFactory(cancellationToken).WithCancellation(cancellationToken))
        {
            var clonedData = CloneArguments(data);
            var factory = new Func<object?[]>(() => CloneArguments(clonedData));
            factories.Add(factory);
            yield return factory;
        }

        if (_isShared)
        {
            _cachedFactories = factories;
        }
    }
}

/// <summary>
/// Task-based delegate data source for methods that return Task<IEnumerable<T>>
/// </summary>
public sealed class TaskDelegateDataSource : AsyncTestDataSource
{
    private readonly Func<Task<IEnumerable<object?[]>>> _taskFactory;
    private readonly bool _isShared;
    private List<Func<object?[]>>? _cachedFactories;

    public override bool IsShared => _isShared;

    public TaskDelegateDataSource(Func<Task<IEnumerable<object?[]>>> taskFactory, bool isShared = false)
    {
        _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
        _isShared = isShared;
    }

    public override async IAsyncEnumerable<Func<object?[]>> GetDataFactoriesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_isShared && _cachedFactories != null)
        {
            foreach (var factory in _cachedFactories)
            {
                yield return factory;
            }
            yield break;
        }

        var factories = new List<Func<object?[]>>();
        
        // Properly await the task
        var dataArrays = await _taskFactory().ConfigureAwait(false);
        
        foreach (var data in dataArrays)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var clonedData = CloneArguments(data);
            var factory = new Func<object?[]>(() => CloneArguments(clonedData));
            factories.Add(factory);
            yield return factory;
        }

        if (_isShared)
        {
            _cachedFactories = factories;
        }
    }
}