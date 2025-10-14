using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Engine.Building;

namespace TUnit.Engine.Models;

/// <summary>
/// Lightweight test metadata for early filtering before building full test objects.
/// Contains only the information needed to apply filters without expensive initialization.
/// </summary>
internal sealed class TestNodeInfo
{
    public required string TestId { get; init; }
    public required string Path { get; init; }
    public required PropertyBag PropertyBag { get; init; }
    public required TestMetadata Metadata { get; init; }
    public required TestBuilder.TestData TestData { get; init; }

    /// <summary>
    /// Whether this test has dependencies that need to be included even if filtered out
    /// </summary>
    public bool HasDependencies => Metadata.Dependencies.Length > 0;
}
