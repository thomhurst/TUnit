using Microsoft.Testing.Platform.Extensions.Messages;

#pragma warning disable TPEXP

namespace TUnit.Engine.Extensions;

internal static class TestNodeStatePropertyExtensions
{
    /// <summary>
    /// Determines whether a state represents a final (terminal) test outcome — i.e. anything
    /// other than the discovery or in-progress placeholder states. Returns <c>false</c> for
    /// <c>null</c>. Shared by the node builder (<see cref="TestExtensions"/>) and the reporters
    /// so the "is this the reportable result?" rule lives in exactly one place.
    /// </summary>
    public static bool IsFinalState(this TestNodeStateProperty? stateProperty)
        => stateProperty is not null and not InProgressTestNodeStateProperty and not DiscoveredTestNodeStateProperty;
}
