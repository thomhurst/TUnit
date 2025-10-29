using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test execution state and lifecycle management
/// Implements <see cref="ITestExecution"/> interface
/// </summary>
public partial class TestContext
{
    // Explicit interface implementations for ITestExecution
    TestPhase ITestExecution.Phase => Phase;
    TestResult? ITestExecution.Result => Result;
    CancellationToken ITestExecution.CancellationToken => CancellationToken;
    DateTimeOffset? ITestExecution.TestStart => TestStart;
    DateTimeOffset? ITestExecution.TestEnd => TestEnd;
    int ITestExecution.CurrentRetryAttempt => CurrentRetryAttempt;
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

    void ITestExecution.OverrideResult(string reason) => OverrideResult(reason);
    void ITestExecution.OverrideResult(TestState state, string reason) => OverrideResult(state, reason);
    void ITestExecution.AddLinkedCancellationToken(CancellationToken cancellationToken) => AddLinkedCancellationToken(cancellationToken);
}
