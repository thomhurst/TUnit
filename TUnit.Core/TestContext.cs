namespace TUnit.Core;

public partial class TestContext : IDisposable
{
    internal EventHandler? OnDispose;
    
    internal readonly TaskCompletionSource TaskCompletionSource = new();
    internal readonly StringWriter OutputWriter = new();
    internal readonly StringWriter ErrorWriter = new();
    
    public Task TestTask => TaskCompletionSource.Task;

    public TestDetails TestDetails { get; }

    public int CurrentRetryAttempt { get; internal set; }

    public List<Timing> Timings { get; } = [];
    public Dictionary<string, object> ObjectBag { get; } = new();

    public TestContext(TestDetails testDetails)
    {
        TestDetails = testDetails;
    }
    
    public TUnitTestResult? Result { get; internal set; }

    public string GetConsoleStandardOutput()
    {
        return OutputWriter.ToString().Trim();
    }
    
    public string GetConsoleErrorOutput()
    {
        return ErrorWriter.ToString().Trim();
    }

    public void Dispose()
    {
        OnDispose?.Invoke(this, EventArgs.Empty);
    }
}