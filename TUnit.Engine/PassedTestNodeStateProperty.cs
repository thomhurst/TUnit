using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Passed test state property
/// </summary>
public class PassedTestNodeStateProperty : IProperty
{
    public static readonly PassedTestNodeStateProperty CachedInstance = new();
}