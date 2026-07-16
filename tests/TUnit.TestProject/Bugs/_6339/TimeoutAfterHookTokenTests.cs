using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6339;

/// <summary>
/// Reproduction for issue #6339: "After(Test) hook failed: The CancellationTokenSource has been disposed."
/// https://github.com/thomhurst/TUnit/issues/6339
///
/// When a test is subject to a timeout (an explicit <c>[Timeout]</c> or a configured
/// <c>DefaultTestTimeout</c>), the engine runs the body with a linked <see cref="CancellationTokenSource"/>
/// token exposed on <c>TestContext.Execution.CancellationToken</c>. That source used to be disposed the
/// moment the body returned, so an <c>[After(Test)]</c> hook that touched the underlying source
/// (e.g. accessing <see cref="CancellationToken.WaitHandle"/> — what a synchronous wait inside ASP.NET Core
/// integration cleanup via EF Core / Respawn / SemaphoreSlim / host shutdown ends up doing) threw
/// <see cref="ObjectDisposedException"/> "The CancellationTokenSource has been disposed."
///
/// Two leaks are covered here, both requiring the timeout source to stay alive through teardown:
/// <list type="number">
/// <item>the After hook reading the context token property directly, and</item>
/// <item>the After hook using a token <b>copy captured during the body</b> — the realistic case, since app
/// code (EF Core, an ASP.NET host) snapshots the ambient token mid-test and only touches it during teardown.
/// Restoring the context property alone does not fix this; the backing source must outlive the hooks.</item>
/// </list>
/// The fix keeps the source alive for the whole per-test lifecycle (disposed once in TestCoordinator after
/// every teardown phase), so later surfacing phases such as tracked-object cleanup are covered too, not just
/// the After(Test) hooks exercised below. The body completes fast (no actual timeout) so this is a plain
/// passing test — the regression is the disposed-token leak into teardown, not cancellation.
/// </summary>
public class TimeoutAfterHookTokenTests
{
    // Snapshot of the ambient timeout token taken during the body, mirroring how app/user code
    // (EF Core, Respawn, an ASP.NET host) captures the token mid-test and touches it later.
    private CancellationToken _capturedDuringBody;

    [Test]
    [Timeout(30_000)]
    [EngineTest(ExpectedResult.Pass)]
    public Task Body_Captures_Timeout_Token_Then_After_Hook_Waits_On_It()
    {
        _capturedDuringBody = TestContext.Current!.Execution.CancellationToken;
        return Task.CompletedTask;
    }

    [After(Test)]
    public void After_Hook_Can_Use_Context_Cancellation_Token(TestContext context)
    {
        // (1) The context property — restored to the still-valid outer token by the fix.
        _ = context.Execution.CancellationToken.WaitHandle;

        // (2) The copy captured during the body — the source behind it must still be alive.
        // Pre-fix this threw ObjectDisposedException because the timeout-scoped source was disposed
        // the instant the body returned. Accessing WaitHandle calls the source's ThrowIfDisposed
        // unconditionally, mirroring a synchronous wait inside real cleanup code.
        _ = _capturedDuringBody.WaitHandle;
    }
}
