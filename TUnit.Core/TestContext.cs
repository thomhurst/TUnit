namespace TUnit.Core;

public partial class TestContext : Context
{
    internal readonly TaskCompletionSource<object?> TaskCompletionSource = new();
    internal readonly List<Artifact> Artifacts = [];

    internal TestContext(TestDetails testDetails)
    {
        TestDetails = testDetails;
    }
    
    public DateTimeOffset? TestStart { get; internal set; }
    
    public Task TestTask => TaskCompletionSource.Task;

    public TestDetails TestDetails { get; }

    public int CurrentRetryAttempt { get; internal set; }

    public List<Timing> Timings { get; } = [];
    public Dictionary<string, object?> ObjectBag { get; } = new();
    
    public TestResult? Result { get; internal set; }
    internal DiscoveredTest InternalDiscoveredTest { get; set; } = null!;
    
    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
}