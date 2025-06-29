using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// In-progress test state property
/// </summary>
public class InProgressTestNodeStateProperty : IProperty
{
    public static readonly InProgressTestNodeStateProperty CachedInstance = new();
}