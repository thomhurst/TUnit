using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6339;

/// <summary>
/// Reproduction for issue #6339: "After(Test) hook failed: The CancellationTokenSource has been disposed."
/// https://github.com/thomhurst/TUnit/issues/6339
///
/// When a test is subject to a timeout (an explicit <c>[Timeout]</c> or a configured
/// <c>DefaultTestTimeout</c>), the engine ran the body with a linked <see cref="CancellationTokenSource"/>
/// token stored on <c>TestContext.Execution.CancellationToken</c>. That source was disposed the moment the
/// body returned, so an <c>[After(Test)]</c> hook that read the context token and did anything source-touching
/// on it (e.g. <see cref="CancellationTokenSource.CreateLinkedTokenSource(CancellationToken)"/>, common in
/// ASP.NET Core integration cleanup via EF Core / Respawn / HttpClient) threw
/// <see cref="ObjectDisposedException"/> "The CancellationTokenSource has been disposed."
///
/// The body completes fast (no actual timeout) so this is a plain passing test — the regression is the
/// disposed-token leak into the After phase, not cancellation. Before the fix the After hook throws and the
/// test is reported failed; after the fix the context token is the still-valid outer token and it passes.
/// </summary>
public class TimeoutAfterHookTokenTests
{
    [Test]
    [Timeout(30_000)]
    [EngineTest(ExpectedResult.Pass)]
    public async Task Body_Completes_Then_After_Hook_Uses_Context_Token()
    {
        await Task.CompletedTask;
    }

    [After(Test)]
    public void After_Hook_Can_Use_Context_Cancellation_Token(TestContext context)
    {
        // Pre-fix: threw ObjectDisposedException "The CancellationTokenSource has been disposed."
        // because the timeout-scoped CTS backing this token was already disposed by TimeoutHelper.
        // Accessing WaitHandle calls the source's ThrowIfDisposed unconditionally, mirroring what
        // synchronous waits inside real cleanup code (EF Core, Respawn, SemaphoreSlim.Wait) hit.
        _ = context.Execution.CancellationToken.WaitHandle;
    }
}
