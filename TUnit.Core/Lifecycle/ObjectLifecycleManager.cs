using System.Collections.Concurrent;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Lifecycle;

/// <summary>
/// Unified implementation of object lifecycle management for TUnit.
/// This is the single source of truth for all object states, replacing
/// the fragmented initialization tracking in ObjectInitializer,
/// DataSourceInitializer._initializationTasks, and PropertyInjectionCache._injectionTasks.
///
/// Thread-safety is ensured through:
/// - ConcurrentDictionary for state storage
/// - TaskCompletionSource pattern for async deduplication
/// - Lock-free algorithms where possible
/// </summary>
internal sealed class ObjectLifecycleManager : IObjectLifecycleManager
{
    /// <summary>
    /// Internal state for each tracked object.
    /// </summary>
    private sealed class ObjectState
    {
        public ObjectLifecyclePhase Phase { get; set; } = ObjectLifecyclePhase.Unknown;
        public int ReferenceCount { get; set; }
        public TaskCompletionSource<bool>? InjectionTask { get; set; }
        public TaskCompletionSource<bool>? InitializationTask { get; set; }
        public List<Action>? DisposalCallbacks { get; set; }
        public List<Func<Task>>? AsyncDisposalCallbacks { get; set; }
        public readonly Lock Lock = new();
    }

    private readonly ConcurrentDictionary<object, ObjectState> _objects = new();
    private readonly IObjectGraphWalker _graphWalker;
    private readonly Disposer _disposer;

    // Delegates for property injection and initialization - set by TUnit.Engine
    private Func<object, ConcurrentDictionary<string, object?>, MethodMetadata?, TestContextEvents?, IDictionary<string, object?>?, CancellationToken, ValueTask>? _propertyInjector;
    private Func<object, CancellationToken, ValueTask>? _asyncInitializer;

    public ObjectLifecycleManager(IObjectGraphWalker graphWalker, Disposer disposer)
    {
        _graphWalker = graphWalker ?? throw new ArgumentNullException(nameof(graphWalker));
        _disposer = disposer ?? throw new ArgumentNullException(nameof(disposer));
    }

    /// <summary>
    /// Configures the property injection delegate.
    /// Called by TUnit.Engine during setup to wire in the property injection logic.
    /// </summary>
    public void SetPropertyInjector(
        Func<object, ConcurrentDictionary<string, object?>, MethodMetadata?, TestContextEvents?, IDictionary<string, object?>?, CancellationToken, ValueTask> injector)
    {
        _propertyInjector = injector ?? throw new ArgumentNullException(nameof(injector));
    }

    /// <summary>
    /// Configures the async initializer delegate.
    /// Called by TUnit.Engine during setup to wire in IAsyncInitializer logic.
    /// </summary>
    public void SetAsyncInitializer(Func<object, CancellationToken, ValueTask> initializer)
    {
        _asyncInitializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
    }

    /// <inheritdoc />
    public ValueTask EnsureRegisteredAsync(object instance, CancellationToken cancellationToken = default)
    {
        var state = GetOrCreateState(instance);

        lock (state.Lock)
        {
            if (state.Phase < ObjectLifecyclePhase.Registered)
            {
                state.Phase = ObjectLifecyclePhase.Registered;
            }
        }

        return default;
    }

    /// <inheritdoc />
    public async ValueTask EnsurePropertiesInjectedAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null,
        IDictionary<string, object?>? preResolvedValues = null,
        CancellationToken cancellationToken = default)
    {
        var state = GetOrCreateState(instance);

        // Fast path: already injected
        if (state.Phase >= ObjectLifecyclePhase.PropertiesInjected)
        {
            return;
        }

        // Use TaskCompletionSource pattern for thread-safe deduplication
        TaskCompletionSource<bool> tcs;
        bool isOwner;

        lock (state.Lock)
        {
            if (state.Phase >= ObjectLifecyclePhase.PropertiesInjected)
            {
                return;
            }

            if (state.InjectionTask != null)
            {
                tcs = state.InjectionTask;
                isOwner = false;
            }
            else
            {
                tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                state.InjectionTask = tcs;
                isOwner = true;
            }
        }

        if (isOwner)
        {
            try
            {
                // Perform property injection
                if (_propertyInjector != null)
                {
                    await _propertyInjector(instance, objectBag, methodMetadata, events, preResolvedValues, cancellationToken).ConfigureAwait(false);
                }

                lock (state.Lock)
                {
                    state.Phase = ObjectLifecyclePhase.PropertiesInjected;
                }

                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
        }
        else
        {
            // Wait for the owning thread to complete
            await tcs.Task.ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask EnsureInitializedAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null,
        IDictionary<string, object?>? preResolvedValues = null,
        CancellationToken cancellationToken = default)
    {
        var state = GetOrCreateState(instance);

        // Fast path: already initialized
        if (state.Phase >= ObjectLifecyclePhase.Initialized)
        {
            return;
        }

        // Ensure properties are injected first
        await EnsurePropertiesInjectedAsync(instance, objectBag, methodMetadata, events, preResolvedValues, cancellationToken).ConfigureAwait(false);

        // Use TaskCompletionSource pattern for thread-safe deduplication
        TaskCompletionSource<bool> tcs;
        bool isOwner;

        lock (state.Lock)
        {
            if (state.Phase >= ObjectLifecyclePhase.Initialized)
            {
                return;
            }

            if (state.InitializationTask != null)
            {
                tcs = state.InitializationTask;
                isOwner = false;
            }
            else
            {
                tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                state.InitializationTask = tcs;
                isOwner = true;
            }
        }

        if (isOwner)
        {
            try
            {
                // Initialize nested objects first (depth-first, deepest first)
                await InitializeNestedObjectsAsync(instance, objectBag, methodMetadata, events, cancellationToken).ConfigureAwait(false);

                // Call IAsyncInitializer if implemented
                if (instance is IAsyncInitializer asyncInitializer)
                {
                    if (_asyncInitializer != null)
                    {
                        await _asyncInitializer(instance, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Fallback: call directly (maintains backward compatibility)
                        await asyncInitializer.InitializeAsync().ConfigureAwait(false);
                    }
                }

                lock (state.Lock)
                {
                    state.Phase = ObjectLifecyclePhase.Initialized;
                }

                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
        }
        else
        {
            // Wait for the owning thread to complete
            await tcs.Task.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Initializes nested objects in depth-first order (deepest first).
    /// </summary>
    private async ValueTask InitializeNestedObjectsAsync(
        object root,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents? events,
        CancellationToken cancellationToken)
    {
        await _graphWalker.WalkGraphAsync(
            root,
            async (obj, depth) =>
            {
                // Skip the root object itself - it will be initialized by the caller
                if (obj == root)
                {
                    return;
                }

                await EnsureInitializedAsync(obj, objectBag, methodMetadata, events, null, cancellationToken).ConfigureAwait(false);
            },
            filter: obj => obj is IAsyncInitializer || PropertyInjection.PropertyInjectionCache.HasInjectableProperties(obj.GetType()),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void IncrementReferenceCount(object instance)
    {
        var state = GetOrCreateState(instance);

        lock (state.Lock)
        {
            state.ReferenceCount++;
            if (state.Phase < ObjectLifecyclePhase.Active)
            {
                state.Phase = ObjectLifecyclePhase.Active;
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DecrementReferenceCountAsync(object instance)
    {
        if (!_objects.TryGetValue(instance, out var state))
        {
            return;
        }

        int newCount;
        List<Action>? syncCallbacks = null;
        List<Func<Task>>? asyncCallbacks = null;

        lock (state.Lock)
        {
            state.ReferenceCount--;
            newCount = state.ReferenceCount;

            if (newCount < 0)
            {
                throw new InvalidOperationException(
                    "Reference count for object went below zero. This indicates a bug in the reference counting logic.");
            }

            if (newCount == 0)
            {
                state.Phase = ObjectLifecyclePhase.Disposing;
                syncCallbacks = state.DisposalCallbacks;
                asyncCallbacks = state.AsyncDisposalCallbacks;
            }
        }

        if (newCount == 0)
        {
            // Execute disposal callbacks
            if (syncCallbacks != null)
            {
                foreach (var callback in syncCallbacks)
                {
                    try
                    {
                        callback();
                    }
                    catch
                    {
                        // Swallow callback exceptions
                    }
                }
            }

            if (asyncCallbacks != null)
            {
                foreach (var callback in asyncCallbacks)
                {
                    try
                    {
                        await callback().ConfigureAwait(false);
                    }
                    catch
                    {
                        // Swallow callback exceptions
                    }
                }
            }

            // Dispose the object
            await _disposer.DisposeAsync(instance).ConfigureAwait(false);

            lock (state.Lock)
            {
                state.Phase = ObjectLifecyclePhase.Disposed;
            }
        }
    }

    /// <inheritdoc />
    public ObjectLifecyclePhase GetPhase(object instance)
    {
        return _objects.TryGetValue(instance, out var state) ? state.Phase : ObjectLifecyclePhase.Unknown;
    }

    /// <inheritdoc />
    public bool IsAtLeast(object instance, ObjectLifecyclePhase phase)
    {
        return GetPhase(instance) >= phase;
    }

    /// <inheritdoc />
    public void OnDisposed(object instance, Action callback)
    {
        var state = GetOrCreateState(instance);

        lock (state.Lock)
        {
            if (state.Phase >= ObjectLifecyclePhase.Disposed)
            {
                // Already disposed, invoke immediately
                callback();
                return;
            }

            state.DisposalCallbacks ??= [];
            state.DisposalCallbacks.Add(callback);
        }
    }

    /// <inheritdoc />
    public void OnDisposedAsync(object instance, Func<Task> callback)
    {
        var state = GetOrCreateState(instance);

        lock (state.Lock)
        {
            if (state.Phase >= ObjectLifecyclePhase.Disposed)
            {
                // Already disposed, invoke immediately
                callback().GetAwaiter().GetResult();
                return;
            }

            state.AsyncDisposalCallbacks ??= [];
            state.AsyncDisposalCallbacks.Add(callback);
        }
    }

    /// <inheritdoc />
    public void ClearSession()
    {
        _objects.Clear();
    }

    /// <summary>
    /// Gets or creates the state for an object.
    /// </summary>
    private ObjectState GetOrCreateState(object instance)
    {
        return _objects.GetOrAdd(instance, _ => new ObjectState());
    }
}
