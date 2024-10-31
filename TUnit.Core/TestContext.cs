namespace TUnit.Core;

public partial class TestContext : Context, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    internal T GetService<T>() => (T) _serviceProvider.GetService(typeof(T))!;
    
    internal readonly TaskCompletionSource<object?> TaskCompletionSource = new();
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
        ObjectBag = originalMetadata.ObjectBag;
    }
    
    public DateTimeOffset? TestStart { get; internal set; }
    
    internal Task TestTask => TaskCompletionSource.Task;

    public TestDetails TestDetails { get; }

    public int CurrentRetryAttempt { get; internal set; }

    public List<ArgumentDisplayFormatter> ArgumentDisplayFormatters { get; } = [];
    
    public List<Timing> Timings { get; } = [];
    public Dictionary<string, object?> ObjectBag { get; }
    
    public TestResult? Result { get; internal set; }
    internal DiscoveredTest InternalDiscoveredTest { get; set; } = null!;

    public void SuppressReportingResult()
    {
        ReportResult = false;
    }
    
    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
    
    public EventHandler? OnDispose { get; set; }

    internal string? SkipReason { get; set; }

    public void Dispose()
    {
        OnDispose?.Invoke(this, EventArgs.Empty);
    }
}