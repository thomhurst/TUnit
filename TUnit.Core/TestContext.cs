namespace TUnit.Core;

public class TestContext : IDisposable
{
    internal EventHandler? OnDispose;

    internal CancellationTokenSource? CancellationTokenSource { get; set; }
    
    public CancellationToken CancellationToken => CancellationTokenSource?.Token ?? default;

    internal readonly StringWriter OutputWriter = new();

    private static readonly AsyncLocal<TestContext> AsyncLocal = new();

    public TestInformation TestInformation { get; }

    internal TestContext(TestInformation testInformation)
    {
        TestInformation = testInformation;
    }

    public static TestContext? Current
    {
        get => AsyncLocal.Value;
        internal set => AsyncLocal.Value = value!;
    }

    public string? SkipReason { get; private set; }

    public void SkipTest(string reason)
    {
        SkipReason = reason;
        CancellationTokenSource?.Cancel();
    }

    public string? FailReason { get; private set; }

    public void FailTest(string reason)
    {
        FailReason = reason;
        CancellationTokenSource?.Cancel();
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