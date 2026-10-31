using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;

namespace TUnit.Engine.Reporters;

/// <summary>
/// Published once at the end of a session that recorded unobserved task exceptions, so the HTML
/// and JUnit reporters can show them at session level.
/// </summary>
internal sealed class UnobservedTaskExceptionsData(SessionUid sessionUid, IReadOnlyList<Exception> exceptions) : IData
{
    public SessionUid SessionUid { get; } = sessionUid;

    public IReadOnlyList<Exception> Exceptions { get; } = exceptions;

    public string DisplayName => "Unobserved task exceptions";

    public string? Description => null;
}
