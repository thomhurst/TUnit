using System.Runtime.CompilerServices;
using TUnit.Core;

namespace TUnit.Core;

/// <summary>
/// Provides direct access to source-generated test metadata
/// </summary>
public static class DirectTestMetadataProvider
{
    private static Func<IReadOnlyList<TestMetadata>>? _metadataProvider;
    private static readonly object _lock = new();

    /// <summary>
    /// Registers the metadata provider from generated code
    /// </summary>
    public static void RegisterMetadataProvider(Func<IReadOnlyList<TestMetadata>> provider)
    {
        lock (_lock)
        {
            _metadataProvider = provider;
        }
    }

    /// <summary>
    /// Gets all test metadata from the generated registry
    /// </summary>
    public static IReadOnlyList<TestMetadata> GetAllTests()
    {
        var provider = _metadataProvider;
        if (provider == null)
        {
            // No generated tests found
            return Array.Empty<TestMetadata>();
        }

        return provider();
    }
}