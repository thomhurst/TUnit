using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;

namespace TUnit.Engine.Tests;

internal sealed class TestSessionContextStub(string sessionUid, CancellationToken cancellationToken = default) : ITestSessionContext
{
    public SessionUid SessionUid { get; } = new(sessionUid);

    public CancellationToken CancellationToken { get; } = cancellationToken;
}
