using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Represents the context for building tests.
/// </summary>
public record TestBuilderContext
{
    private static readonly AsyncLocal<TestBuilderContext?> BuilderContexts = new();

    /// <summary>
    /// Gets the current test builder context.
    /// </summary>
    public static TestBuilderContext? Current
    {
        get => BuilderContexts.Value;
        internal set => BuilderContexts.Value = value;
    }

    public string DefinitionId { get; } = Guid.NewGuid().ToString();

     [Obsolete("Use StateBag property instead.")]
     public ConcurrentDictionary<string, object?> ObjectBag => StateBag;

    /// <summary>
    /// Gets the state bag for storing arbitrary data during test building.
    /// </summary>
    public ConcurrentDictionary<string, object?> StateBag { get; set; } = new();

    public TestContextEvents Events { get; set; } = new();

    public IDataSourceAttribute? DataSourceAttribute { get; set; }

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

    public TestBuilderContextAccessor(TestBuilderContext context)
    {
        _current = context;
        TestBuilderContext.Current = context;
    }

    public TestBuilderContext Current
    {
        get => _current;
        set
        {
            _current = value;
            TestBuilderContext.Current = value;
        }
    }
}
