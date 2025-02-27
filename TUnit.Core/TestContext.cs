namespace TUnit.Core;

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

    public TestContextEvents Events { get; }
    
    public bool IsRegistered { get; internal set; }

    public DateTimeOffset? TestStart { get; internal set; }
    
    internal Task? TestTask { get; set; }

    public TestDetails TestDetails { get; }

    public int CurrentRetryAttempt { get; internal set; }

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