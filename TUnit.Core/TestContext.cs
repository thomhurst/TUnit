namespace TUnit.Core;

public partial class TestContext : IDisposable
{
    internal readonly TaskCompletionSource<object?> TaskCompletionSource = new();
    internal readonly List<Artifact> Artifacts = [];

    internal TestContext(TestDetails testDetails)
    {
        TestDetails = testDetails;
    }
    
    public StringWriter Out { get; } = new();
    
    public Task TestTask => TaskCompletionSource.Task;

    public TestDetails TestDetails { get; }

    public int CurrentRetryAttempt { get; internal set; }

    public List<Timing> Timings { get; } = [];
    public Dictionary<string, object?> ObjectBag { get; } = new();
    
    public TUnitTestResult? Result { get; internal set; }

    public string GetTestOutput()
    {
        return Out.ToString().Trim();
    }
    
    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }

    public void Dispose()
    {
        Out.Dispose();
    }
}