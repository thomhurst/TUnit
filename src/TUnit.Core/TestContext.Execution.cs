using TUnit.Core.Enums;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test execution state and lifecycle management
/// Implements <see cref="ITestExecution"/> interface
/// </summary>
public partial class TestContext
{
    // Internal backing fields and properties
    private CancellationToken _baseCancellationToken;
    private CancellationTokenSource? _testCancellationTokenSource;
    private List<CancellationTokenSource>? _retiredTestCancellationTokenSources;
    private bool _acceptingTestCancellation;
    private volatile bool _testCancellationRequested;
    private List<CancellationToken>? _externalCancellationTokens;
    private CancellationTokenSource? _linkedCancellationTokenSource;
    // Token copies can escape into user code and remain in use through teardown (#6339).
    // Keep replaced sources alive until the complete test lifecycle has finished.
    private List<CancellationTokenSource>? _retiredLinkedCancellationTokenSources;
    internal CancellationToken CancellationToken { get; private set; }

    // Linked source backing the per-test timeout token. Owned for the whole test lifecycle — the body
    // plus every teardown phase (After(Test) hooks, instance/OnDispose, object cleanup, After(Class)/
    // After(Assembly)) — and disposed once at the end in TestCoordinator. Keeping it alive that long
    // means a token copy captured mid-body and later touched by a synchronous .WaitHandle wait never
    // observes a disposed CancellationTokenSource (#6339). Only allocated for tests that have a timeout.
    internal CancellationTokenSource? TimeoutCancellationSource { get; set; }
    internal TestPhase Phase { get; set; } = TestPhase.Execution;
    internal TestResult? Result { get; set; }
    internal string? SkipReason { get; set; }
    internal DateTimeOffset? TestStart { get; set; }
    internal DateTimeOffset? TestEnd { get; set; }
    internal int CurrentRetryAttempt { get; set; }
    // Lazily allocated; stays null for the common no-retry case so passing tests pay nothing.
    internal List<TestResult>? RetryAttempts { get; set; }
    internal Func<TestContext, Exception, int, Task<bool>>? RetryFunc { get; set; }
    internal IHookExecutor? CustomHookExecutor { get; set; }
    internal bool ReportResult { get; set; } = true;
    internal bool IsNotDiscoverable { get; set; }
    internal bool IsTestCancellationRequested => _testCancellationRequested;
    internal CancellationToken TestCancellationToken => _testCancellationTokenSource?.Token ?? CancellationToken;

    // Explicit interface implementations for ITestExecution
    TestPhase ITestExecution.Phase => Phase;
    TestResult? ITestExecution.Result
    {
        get => Result;
        set => Result = value;
    }

    CancellationToken ITestExecution.CancellationToken => CancellationToken;
    DateTimeOffset? ITestExecution.TestStart
    {
        get => TestStart;
        set => TestStart = value;
    }

    DateTimeOffset? ITestExecution.TestEnd
    {
        get => TestEnd;
        set => TestEnd = value;
    }

    int ITestExecution.CurrentRetryAttempt
    {
        get => CurrentRetryAttempt;
        set => CurrentRetryAttempt = value;
    }

    // Array.Empty for the common no-retry path so passing tests allocate nothing.
    IReadOnlyList<TestResult> ITestExecution.RetryAttempts
        => (IReadOnlyList<TestResult>?)RetryAttempts ?? Array.Empty<TestResult>();

    string? ITestExecution.SkipReason => SkipReason;
    Func<TestContext, Exception, int, Task<bool>>? ITestExecution.RetryFunc => RetryFunc;
    IHookExecutor? ITestExecution.CustomHookExecutor
    {
        get => CustomHookExecutor;
        set => CustomHookExecutor = value;
    }
    bool ITestExecution.ReportResult
    {
        get => ReportResult;
        set => ReportResult = value;
    }
    bool ITestExecution.IsNotDiscoverable
    {
        get => IsNotDiscoverable;
        set => IsNotDiscoverable = value;
    }

    void ITestExecution.OverrideResult(TestState state, string reason) => OverrideResult(state, reason);
    void ITestExecution.Cancel() => Cancel();
    void ITestExecution.AddLinkedCancellationToken(CancellationToken cancellationToken) => AddLinkedCancellationToken(cancellationToken);

    // Internal implementation methods
    internal void OverrideResult(TestState state, string reason)
    {
        lock (Lock)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Override reason cannot be empty or whitespace.", nameof(reason));
            }

            if (Result?.IsOverridden == true)
            {
                throw new InvalidOperationException(
                    $"Result has already been overridden to {Result.State} with reason: '{Result.OverrideReason}'. " +
                    "Cannot override a result multiple times. Check Result.IsOverridden before calling OverrideResult().");
            }

            if (state is TestState.NotStarted or TestState.WaitingForDependencies or TestState.Queued or TestState.Running)
            {
                throw new ArgumentException(
                    $"Cannot override to intermediate state '{state}'. " +
                    "Only final states (Passed, Failed, Skipped, Timeout, Cancelled) are allowed.",
                    nameof(state));
            }

            var originalException = Result?.Exception;

            Exception? exceptionForResult;
            if (state == TestState.Failed)
            {
                exceptionForResult = originalException ?? new InvalidOperationException($"Test overridden to failed: {reason}");
            }
            else
            {
                exceptionForResult = null;
            }

            Result = new TestResult
            {
                State = state,
                OverrideReason = reason,
                IsOverridden = true,
                OriginalException = originalException,
                Start = TestStart ?? DateTimeOffset.UtcNow,
                End = DateTimeOffset.UtcNow,
                Duration = DateTimeOffset.UtcNow - (TestStart ?? DateTimeOffset.UtcNow),
                Exception = exceptionForResult,
                ComputerName = EnvironmentHelper.MachineName,
                TestContext = this
            };

            InternalExecutableTest.State = state;
        }
    }

    internal void AddLinkedCancellationToken(CancellationToken cancellationToken)
    {
        lock (Lock)
        {
            (_externalCancellationTokens ??= []).Add(cancellationToken);
            RebuildLinkedCancellationTokenSource();
        }
    }

    internal void InitializeTestCancellation(CancellationToken cancellationToken)
    {
        lock (Lock)
        {
            if (_testCancellationTokenSource is { } previousTestCancellationTokenSource)
            {
                (_retiredTestCancellationTokenSources ??= []).Add(previousTestCancellationTokenSource);
            }

            _testCancellationTokenSource = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : new CancellationTokenSource();
            _acceptingTestCancellation = true;
            _testCancellationRequested = false;
            _baseCancellationToken = _testCancellationTokenSource.Token;
            RebuildLinkedCancellationTokenSource();
        }
    }

    internal bool CompleteTestCancellation()
    {
        lock (Lock)
        {
            _acceptingTestCancellation = false;
            return _testCancellationRequested;
        }
    }

    internal void RestoreTestCancellationToken()
    {
        lock (Lock)
        {
            if (_testCancellationTokenSource is not null)
            {
                _baseCancellationToken = _testCancellationTokenSource.Token;
                RebuildLinkedCancellationTokenSource();
            }
        }
    }

    internal void Cancel()
    {
        CancellationTokenSource cancellationTokenSource;

        lock (Lock)
        {
            if (!_acceptingTestCancellation || _testCancellationTokenSource is null)
            {
                return;
            }

            _testCancellationRequested = true;
            cancellationTokenSource = _testCancellationTokenSource;
        }

        try
        {
            cancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Final cleanup can dispose the source after we release Lock but before Cancel runs.
            // Treat that race as a late cancellation request, which is intentionally a no-op.
        }
    }

    internal void SetCancellationToken(CancellationToken cancellationToken)
    {
        lock (Lock)
        {
            if (_baseCancellationToken == cancellationToken)
            {
                return;
            }

            _baseCancellationToken = cancellationToken;
            RebuildLinkedCancellationTokenSource();
        }
    }

    private void RebuildLinkedCancellationTokenSource()
    {
        if (_externalCancellationTokens is not { Count: > 0 } externalCancellationTokens)
        {
            CancellationToken = _baseCancellationToken;
            return;
        }

        CancellationTokenSource linkedCancellationTokenSource;
        if (externalCancellationTokens.Count == 1)
        {
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                _baseCancellationToken,
                externalCancellationTokens[0]);
        }
        else
        {
            var cancellationTokens = new CancellationToken[externalCancellationTokens.Count + 1];
            cancellationTokens[0] = _baseCancellationToken;
            externalCancellationTokens.CopyTo(cancellationTokens, 1);
            linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens);
        }

        if (_linkedCancellationTokenSource is { } previousLinkedCancellationTokenSource)
        {
            (_retiredLinkedCancellationTokenSources ??= []).Add(previousLinkedCancellationTokenSource);
        }

        _linkedCancellationTokenSource = linkedCancellationTokenSource;
        CancellationToken = linkedCancellationTokenSource.Token;
    }

    internal void DisposeLinkedCancellationTokenSources()
    {
        lock (Lock)
        {
            _linkedCancellationTokenSource?.Dispose();
            _linkedCancellationTokenSource = null;

            if (_retiredLinkedCancellationTokenSources is { } retiredLinkedCancellationTokenSources)
            {
                for (var i = retiredLinkedCancellationTokenSources.Count - 1; i >= 0; i--)
                {
                    retiredLinkedCancellationTokenSources[i].Dispose();
                }

                _retiredLinkedCancellationTokenSources = null;
            }

            _externalCancellationTokens = null;
            CancellationToken = _baseCancellationToken;

            _testCancellationTokenSource?.Dispose();
            _testCancellationTokenSource = null;

            if (_retiredTestCancellationTokenSources is { } retiredTestCancellationTokenSources)
            {
                for (var i = retiredTestCancellationTokenSources.Count - 1; i >= 0; i--)
                {
                    retiredTestCancellationTokenSources[i].Dispose();
                }

                _retiredTestCancellationTokenSources = null;
            }

            _acceptingTestCancellation = false;
        }
    }
}
