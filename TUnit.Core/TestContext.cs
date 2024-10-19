namespace TUnit.Core;

public partial class TestContext : Context, IDisposable
{
    internal readonly TaskCompletionSource<object?> TaskCompletionSource = new();
    internal readonly List<Artifact> Artifacts = [];
#if NET9_0_OR_GREATER
    public readonly Lock Lock = new();
#else
    public readonly object Lock = new();
#endif

    internal TestContext(TestDetails testDetails, Dictionary<string, object?> objectBag)
    {
        TestDetails = testDetails;
        ObjectBag = objectBag;
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
    
    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
    
    public EventHandler? OnDispose { get; set; }
    
    public void Dispose()
    {
        OnDispose?.Invoke(this, EventArgs.Empty);
    }
}