using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Discovered test state property
/// </summary>
public class DiscoveredTestNodeStateProperty : IProperty
{
    public static readonly DiscoveredTestNodeStateProperty CachedInstance = new();
}