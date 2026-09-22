using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Represents the context for building tests.
/// </summary>
public record TestBuilderContext
{
    private static readonly AsyncLocal<TestBuilderContext?> BuilderContexts = new();
    private string? _definitionId;

    /// <summary>
    /// Gets the current test builder context.
    /// </summary>
    public static TestBuilderContext? Current
    {
        get => BuilderContexts.Value;
        internal set => BuilderContexts.Value = value;
    }

    /// <summary>
    /// Gets the unique definition ID for this context. Generated lazily on first access.
    /// </summary>
    public string DefinitionId => _definitionId ??= Guid.NewGuid().ToString();

    private ConcurrentDictionary<string, object?>? _stateBag;
    private TestBuilderContext? _stateBagSource;
    private TestContextEvents? _events;

    /// <summary>
    /// Gets the state bag for storing arbitrary data during test building.
    /// </summary>
    public ConcurrentDictionary<string, object?> StateBag
    {
        get => Volatile.Read(ref _stateBag) ?? GetOrCreateStateBag();
        set
        {
            _stateBagSource = null;
            Volatile.Write(ref _stateBag, value);
        }
    }

    // Slow path kept out of the getter so the common "already created" read stays inlineable.
    // A context sharing another context's bag (see ShareStateBagWith) resolves it from that
    // source on first access instead of forcing the source to allocate one up front — most
    // tests never touch the bag, and a ConcurrentDictionary costs ~1KB (per-core lock array).
    private ConcurrentDictionary<string, object?> GetOrCreateStateBag()
    {
        var created = _stateBagSource?.StateBag ?? new ConcurrentDictionary<string, object?>();
        return Interlocked.CompareExchange(ref _stateBag, created, null) ?? created;
    }

    /// <summary>
    /// Makes this context use <paramref name="source"/>'s state bag without materializing it.
    /// Equivalent to <c>StateBag = source.StateBag</c>, except the bag is only created
    /// (on the source) when either context first reads it.
    /// </summary>
    internal void ShareStateBagWith(TestBuilderContext source)
    {
        if (Volatile.Read(ref source._stateBag) is { } existing)
        {
            StateBag = existing;
            return;
        }

        Volatile.Write(ref _stateBag, null);
        _stateBagSource = source;
    }

    /// <summary>
    /// True when a state bag has been materialized for this context (directly or via the context it shares with).
    /// </summary>
    internal bool HasStateBag => Volatile.Read(ref _stateBag) is not null || _stateBagSource is { HasStateBag: true };

    /// <inheritdoc cref="StateBag"/>
    [Obsolete("Use StateBag property instead.")]
    public ConcurrentDictionary<string, object?> ObjectBag => StateBag;

    internal void CopyStateBagTo(TestBuilderContext target)
    {
        if (HasStateBag && StateBag is { IsEmpty: false } bag)
        {
            target.StateBag = new ConcurrentDictionary<string, object?>(bag);
        }
    }

    public TestContextEvents Events
    {
        get => _events ??= new TestContextEvents();
        set => _events = value;
    }

    /// <summary>
    /// The events container if something has created it, otherwise <c>null</c>. For read-only
    /// consumers that would otherwise allocate an empty container just to find no subscribers.
    /// </summary>
    internal TestContextEvents? EventsIfCreated => _events;

    /// <summary>
    /// Gets or sets the data source attribute that generated the test's method arguments, if any.
    /// </summary>
    public IDataSourceAttribute? DataSourceAttribute { get; set; }

    /// <summary>
    /// Gets or sets the data source attribute that generated the test's class (constructor) arguments, if any.
    /// </summary>
    public IDataSourceAttribute? ClassDataSourceAttribute { get; set; }

    /// <summary>
    /// Gets the test method information, if available during source generation.
    /// </summary>
    public required MethodMetadata TestMetadata { get; init; }

    internal IClassConstructor? ClassConstructor { get; set; }

    /// <summary>
    /// Cached and initialized attributes for the test
    /// </summary>
    internal Attribute[]? InitializedAttributes { get; set; }

    public void RegisterForInitialization(object? obj)
    {
        Events.OnInitialize += async (sender, args) =>
        {
            // Discovery: only IAsyncDiscoveryInitializer
            await ObjectInitializer.InitializeForDiscoveryAsync(obj);
        };
    }

    internal static TestBuilderContext FromTestContext(TestContext testContext, IDataSourceAttribute? dataSourceAttribute)
    {
        return new TestBuilderContext
        {
            Events = testContext.InternalEvents,
            TestMetadata = testContext.Metadata.TestDetails.MethodMetadata,
            DataSourceAttribute = dataSourceAttribute,
            ClassDataSourceAttribute = testContext.Metadata.ClassDataSource,
            StateBag = testContext.StateBag.Items,
            ClassConstructor = testContext.ClassConstructor,
        };
    }
}

/// <summary>
/// Provides access to the current <see cref="TestBuilderContext"/>.
/// </summary>
public class TestBuilderContextAccessor
{
    private TestBuilderContext _current;
    private readonly bool _publishToAsyncLocal = true;

    public TestBuilderContextAccessor(TestBuilderContext context)
    {
        _current = context;
        TestBuilderContext.Current = context;
    }

    /// <summary>
    /// Creates an accessor that only tracks the current context without publishing it to
    /// <see cref="TestBuilderContext.Current"/>. Used by the engine when no user code can run while
    /// the accessor is live (no data sources / class constructor), saving an ExecutionContext
    /// allocation per AsyncLocal write.
    /// </summary>
    internal TestBuilderContextAccessor(TestBuilderContext context, bool publishToAsyncLocal)
    {
        _current = context;
        _publishToAsyncLocal = publishToAsyncLocal;

        if (publishToAsyncLocal)
        {
            TestBuilderContext.Current = context;
        }
    }

    public TestBuilderContext Current
    {
        get => _current;
        set
        {
            _current = value;

            if (_publishToAsyncLocal)
            {
                TestBuilderContext.Current = value;
            }
        }
    }
}
