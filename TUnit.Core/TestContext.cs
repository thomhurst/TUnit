namespace TUnit.Core;

/// <summary>
/// Represents the context for a test.
/// </summary>
public partial class TestContext : Context
{
    private readonly IServiceProvider _serviceProvider;

    internal T GetService<T>() => (T) _serviceProvider.GetService(typeof(T))!;
    
    internal readonly List<Artifact> Artifacts = [];
    internal readonly List<CancellationToken> LinkedCancellationTokens = [];
    internal readonly TestMetadata OriginalMetadata;

#if NET9_0_OR_GREATER
    public readonly Lock Lock = new();
#else
    public readonly object Lock = new();
#endif

    internal bool ReportResult = true;
    
    internal TestContext(IServiceProvider serviceProvider, TestDetails testDetails, TestMetadata originalMetadata)
    {
        _serviceProvider = serviceProvider;
        OriginalMetadata = originalMetadata;
        TestDetails = testDetails;
        ObjectBag = originalMetadata.TestBuilderContext.ObjectBag;
        Events = originalMetadata.TestBuilderContext.Events;
    }

    /// <summary>
    /// Gets the events associated with the test context.
    /// </summary>
    public TestContextEvents Events { get; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the test is registered.
    /// </summary>
    public bool IsRegistered { get; internal set; }

    /// <summary>
    /// Gets or sets the start time of the test.
    /// </summary>
    public DateTimeOffset? TestStart { get; internal set; }
    
    internal Task? TestTask { get; set; }

    /// <summary>
    /// Gets the details of the test.
    /// </summary>
    public TestDetails TestDetails { get; }

    /// <summary>
    /// Gets or sets the current retry attempt for the test.
    /// </summary>
    public int CurrentRetryAttempt { get; internal set; }

    /// <summary>
    /// Gets the argument display formatters for the test.
    /// </summary>
    public List<ArgumentDisplayFormatter> ArgumentDisplayFormatters { get; } = [];
    
    public List<Timing> Timings { get; } = [];
    public Dictionary<string, object?> ObjectBag { get; }
    
    public TestResult? Result { get; internal set; }
    
    public CancellationToken CancellationToken { get; internal set; }
    
    internal DiscoveredTest InternalDiscoveredTest { get; set; } = null!;

    public void SuppressReportingResult()
    {
        ReportResult = false;
    }
    
    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
    
    internal string? SkipReason { get; set; }
    
    internal object?[]? EventObjects { get; set; }
}