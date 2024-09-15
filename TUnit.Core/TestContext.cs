namespace TUnit.Core;

public partial class TestContext
{
    internal readonly TaskCompletionSource<object?> TaskCompletionSource = new();
    internal readonly List<Artifact> Artifacts = [];

    internal TestContext(TestDetails testDetails)
    {
        TestDetails = testDetails;
    }
    
    public DateTimeOffset? TestStart { get; internal set; }
    
    public StringWriter OutputWriter { get; } = new();
    public StringWriter ErrorOutputWriter { get; } = new();
    
    public Task TestTask => TaskCompletionSource.Task;

    public TestDetails TestDetails { get; }

    public int CurrentRetryAttempt { get; internal set; }

    public List<Timing> Timings { get; } = [];
    public Dictionary<string, object?> ObjectBag { get; } = new();
    
    public TestResult? Result { get; internal set; }
    internal DiscoveredTest InternalDiscoveredTest { get; set; } = null!;

    public string GetTestOutput()
    {
        return OutputWriter.GetStringBuilder().ToString().Trim();
    }
    
    public string GetTestErrorOutput()
    {
        return ErrorOutputWriter.GetStringBuilder().ToString().Trim();
    }
    
    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
}