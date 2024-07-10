namespace TUnit.Core;

public partial class TestContext : IDisposable
{
    internal EventHandler? OnDispose;
    private CancellationTokenSource? _cancellationTokenSource;
    
    internal readonly TaskCompletionSource TaskCompletionSource = new();
    internal readonly StringWriter OutputWriter = new();
    internal readonly StringWriter ErrorWriter = new();

    internal CancellationTokenSource? CancellationTokenSource
    {
        get => _cancellationTokenSource;
        set
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = value;
        }
    }

    public CancellationToken CancellationToken => CancellationTokenSource?.Token ?? default;

    public Task TestTask => TaskCompletionSource.Task;

    public TestDetails TestDetails { get; }

    public int CurrentRetryAttempt { get; internal set; }

    public DateTimeOffset? SetUpStart { get; internal set; }
    public DateTimeOffset? SetUpEnd { get; internal set; }
    
    public DateTimeOffset? TestStart { get; internal set; }
    public DateTimeOffset? TestEnd { get; internal set; }
    
    public DateTimeOffset? CleanUpStart { get; internal set; }
    public DateTimeOffset? CleanUpEnd { get; internal set; }
    
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
        OutputWriter.Dispose();
        ErrorWriter.Dispose();
        CancellationTokenSource?.Dispose();
    }
}