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
    internal CancellationToken CancellationToken { get; set; }
    internal CancellationTokenSource? LinkedCancellationTokens { get; set; }
    internal TestPhase Phase { get; set; } = TestPhase.Execution;
    internal TestResult? Result { get; set; }
    internal string? SkipReason { get; set; }
    internal DateTimeOffset? TestStart { get; set; }
    internal DateTimeOffset? TestEnd { get; set; }
    internal int CurrentRetryAttempt { get; set; }
    internal Func<TestContext, Exception, int, Task<bool>>? RetryFunc { get; set; }
    internal IHookExecutor? CustomHookExecutor { get; set; }
    internal bool ReportResult { get; set; } = true;

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

    void ITestExecution.OverrideResult(TestState state, string reason) => OverrideResult(state, reason);
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

            var now = TimeProviderContext.Current.GetUtcNow();
            Result = new TestResult
            {
                State = state,
                OverrideReason = reason,
                IsOverridden = true,
                OriginalException = originalException,
                Start = TestStart ?? now,
                End = now,
                Duration = now - (TestStart ?? now),
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
            if (LinkedCancellationTokens == null)
            {
                LinkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
            }
            else
            {
                var existingToken = LinkedCancellationTokens.Token;
                var oldCts = LinkedCancellationTokens;
                LinkedCancellationTokens = CancellationTokenSource.CreateLinkedTokenSource(existingToken, cancellationToken);
                oldCts.Dispose();
            }

            CancellationToken = LinkedCancellationTokens.Token;
        }
    }
}
