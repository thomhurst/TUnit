namespace TUnit.Core;

public partial class TestContext : IDisposable
{
    internal EventHandler? OnDispose;
    private CancellationTokenSource? _cancellationTokenSource;
    
    internal readonly TaskCompletionSource _taskCompletionSource = new();
    internal readonly StringWriter OutputWriter = new();

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

    public Task TestTask => _taskCompletionSource.Task;

    public TestInformation TestInformation { get; }

    public Dictionary<string, object> ObjectBag { get; } = new();

    public TestContext(TestInformation testInformation)
    {
        TestInformation = testInformation;
    }
    
    public TUnitTestResult? Result { get; internal set; }

    public string GetConsoleOutput()
    {
        return OutputWriter.ToString().Trim();
    }

    public void Dispose()
    {
        OnDispose?.Invoke(this, EventArgs.Empty);
        OutputWriter.Dispose();
        CancellationTokenSource?.Dispose();
    }
}