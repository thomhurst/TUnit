using FsCheck;
using FsCheck.Fluent;
using TUnit.Core;

namespace TUnit.FsCheck;

/// <summary>
/// Default <see cref="Arbitrary{T}"/> for <see cref="CancellationToken"/> used by
/// <see cref="FsCheckPropertyTestExecutor"/>. Returns the timeout-backed token exposed by
/// <see cref="TestContext.Current"/>, or <see cref="CancellationToken.None"/> when no test
/// context is in scope.
/// </summary>
/// <remarks>
/// Without this registration, FsCheck's built-in reflection-based arbitrary for
/// <see cref="CancellationToken"/> invokes the <c>CancellationToken(bool canceled)</c>
/// constructor with a randomly generated bool, producing a pre-cancelled token on roughly
/// half of invocations. A property body that awaits using that token then fails immediately
/// with <see cref="OperationCanceledException"/>, silently defeating <see cref="TimeoutAttribute"/>
/// cooperation.
/// Users may still register their own <see cref="Arbitrary{T}"/> via
/// <see cref="FsCheckPropertyAttribute.Arbitrary"/>; user registrations take precedence
/// because they appear earlier in the type list passed to FsCheck's <c>WithArbitrary</c>.
/// </remarks>
internal static class CancellationTokenArbitrary
{
    /// <summary>Produces the current test's CancellationToken, or <see cref="CancellationToken.None"/>.</summary>
    public static Arbitrary<CancellationToken> CancellationToken()
    {
        var token = TestContext.Current?.Execution.CancellationToken ?? System.Threading.CancellationToken.None;
        return Arb.From(Gen.Constant(token));
    }
}
