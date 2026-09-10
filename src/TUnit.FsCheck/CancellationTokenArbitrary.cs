using FsCheck;
using FsCheck.Fluent;
using TUnit.Core;

namespace TUnit.FsCheck;

/// <summary>
/// Default <see cref="Arbitrary{T}"/> for <see cref="CancellationToken"/>; surfaces the
/// timeout-backed token from <see cref="TestContext.Current"/>.
/// </summary>
internal static class CancellationTokenArbitrary
{
    public static Arbitrary<CancellationToken> CancellationToken()
    {
        // Called by FsCheck via reflection during property execution; TestContext.Current is in scope.
        var token = TestContext.Current?.Execution.CancellationToken ?? System.Threading.CancellationToken.None;
        return Arb.From(Gen.Constant(token));
    }
}
