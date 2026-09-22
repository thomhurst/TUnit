namespace TUnit.Core;

/// <summary>
/// Context for capturing output during test building and data source initialization.
/// This context is active during the test building phase, before TestContext is created.
/// Output captured here is transferred to the TestContext when it's created.
/// </summary>
public sealed class TestBuildContext : Context, IDisposable
{
    private static readonly AsyncLocal<TestBuildContext?> _current = new();

    public static new TestBuildContext? Current
    {
        get => _current.Value;
        internal set => _current.Value = value;
    }

    public TestBuildContext() : base(null)
    {
    }

    /// <summary>
    /// Gets the captured standard output during test building.
    /// </summary>
    public string GetCapturedOutput() => GetStandardOutput();

    /// <summary>
    /// Gets the captured error output during test building.
    /// </summary>
    public string GetCapturedErrorOutput() => GetErrorOutput();

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }

    /// <summary>
    /// Clears the current TestBuildContext.
    /// </summary>
    public new void Dispose()
    {
        Current = null;
        base.Dispose();
    }

    /// <summary>
    /// Releases the context's resources without writing <see cref="Current"/>. For callers that
    /// own the async flow the context was published in and are about to leave it (the flow's
    /// AsyncLocal changes are discarded on return anyway), so clearing it would only cost an
    /// extra ExecutionContext allocation.
    /// </summary>
    internal void DisposeWithoutClearingCurrent() => base.Dispose();
}
