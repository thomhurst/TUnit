using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Represents the context for building tests.
/// </summary>
public record TestBuilderContext
{
    private static readonly AsyncLocal<TestBuilderContext?> BuilderContexts = new();
    private readonly StringBuilder _outputBuilder = new();
    private readonly StringBuilder _errorOutputBuilder = new();
    private readonly ReaderWriterLockSlim _outputLock = new(LockRecursionPolicy.NoRecursion);
    private readonly ReaderWriterLockSlim _errorOutputLock = new(LockRecursionPolicy.NoRecursion);

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

    [field: AllowNull, MaybeNull]
    public TextWriter OutputWriter => field ??= new ConcurrentStringWriter(_outputBuilder, _outputLock);

    [field: AllowNull, MaybeNull]
    public TextWriter ErrorOutputWriter => field ??= new ConcurrentStringWriter(_errorOutputBuilder, _errorOutputLock);

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
            await ObjectInitializer.InitializeAsync(obj);
        };
    }

    internal static TestBuilderContext FromTestContext(TestContext testContext, IDataSourceAttribute? dataSourceAttribute)
    {
        return new TestBuilderContext
        {
            Events = testContext.InternalEvents, TestMetadata = testContext.Metadata.TestDetails.MethodMetadata, DataSourceAttribute = dataSourceAttribute, StateBag = testContext.StateBag.Items,
        };
    }

    /// <summary>
    /// Gets the captured standard output from this builder context.
    /// </summary>
    internal string GetCapturedOutput()
    {
        if (_outputBuilder.Length == 0)
        {
            return string.Empty;
        }

        _outputLock.EnterReadLock();
        try
        {
            return _outputBuilder.ToString();
        }
        finally
        {
            _outputLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the captured error output from this builder context.
    /// </summary>
    internal string GetCapturedErrorOutput()
    {
        if (_errorOutputBuilder.Length == 0)
        {
            return string.Empty;
        }

        _errorOutputLock.EnterReadLock();
        try
        {
            return _errorOutputBuilder.ToString();
        }
        finally
        {
            _errorOutputLock.ExitReadLock();
        }
    }
}

/// <summary>
/// Provides access to the current <see cref="TestBuilderContext"/>.
/// </summary>
public class TestBuilderContextAccessor(TestBuilderContext context)
{
    public TestBuilderContext Current { get; set; } = context;
}
