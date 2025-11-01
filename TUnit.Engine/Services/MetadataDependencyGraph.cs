using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Builds and analyzes dependency relationships between test metadata
/// without requiring tests to be built. This enables performance optimization
/// by determining which tests need to be built based on filtering and dependencies.
/// </summary>
internal sealed class MetadataDependencyGraph
{
    private readonly Dictionary<TestMetadata, List<TestMetadata>> _dependencyMap = [];
    private readonly Dictionary<TestMetadata, List<TestMetadata>> _dependentMap = [];

    /// <summary>
    /// Builds the dependency graph from all test metadata.
    /// Creates both forward dependencies (what this test depends on) and
    /// reverse dependencies (what tests depend on this test).
    /// </summary>
    public void Build(IEnumerable<TestMetadata> allMetadata)
    {
        var metadataList = allMetadata.ToList();

        // Build forward and reverse dependency maps
        foreach (var metadata in metadataList)
        {
            var dependencies = new List<TestMetadata>();

            // Find all metadata that match this test's dependency specifications
            foreach (var dependencySpec in metadata.Dependencies)
            {
                var matches = FindMatchingMetadata(metadataList, dependencySpec, metadata);
                dependencies.AddRange(matches);
            }

            _dependencyMap[metadata] = dependencies;

            // Build reverse map - track which tests depend on each metadata
            foreach (var dependency in dependencies)
            {
                if (!_dependentMap.TryGetValue(dependency, out var dependents))
                {
                    dependents = [];
                    _dependentMap[dependency] = dependents;
                }
                dependents.Add(metadata);
            }
        }
    }

    /// <summary>
    /// Finds all metadata that match a dependency specification.
    /// Uses the existing TestDependency.Matches logic which works with metadata directly.
    /// </summary>
    private List<TestMetadata> FindMatchingMetadata(
        List<TestMetadata> allMetadata,
        TestDependency dependencySpec,
        TestMetadata dependentMetadata)
    {
        var matches = new List<TestMetadata>();

        foreach (var candidate in allMetadata)
        {
            // Use existing TestDependency.Matches logic!
            // This method can work with TestMetadata directly without building tests
            if (dependencySpec.Matches(candidate, dependentMetadata))
            {
                matches.Add(candidate);
            }
        }

        return matches;
    }

    /// <summary>
    /// Expands a set of root metadata to include all transitive dependencies.
    /// Performs a breadth-first traversal of the dependency graph.
    /// </summary>
    /// <param name="roots">The initial set of metadata (e.g., filtered tests)</param>
    /// <returns>Complete set including all transitive dependencies</returns>
    public HashSet<TestMetadata> ExpandDependencies(IEnumerable<TestMetadata> roots)
    {
        var result = new HashSet<TestMetadata>();
        var queue = new Queue<TestMetadata>(roots);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!result.Add(current))
            {
                continue; // Already processed
            }

            // Add all dependencies of current test
            if (_dependencyMap.TryGetValue(current, out var dependencies))
            {
                foreach (var dep in dependencies)
                {
                    if (!result.Contains(dep))
                    {
                        queue.Enqueue(dep);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the direct dependencies for a given metadata.
    /// </summary>
    public IReadOnlyList<TestMetadata> GetDependencies(TestMetadata metadata)
    {
        return _dependencyMap.TryGetValue(metadata, out var dependencies)
            ? dependencies
            : Array.Empty<TestMetadata>();
    }

    /// <summary>
    /// Gets all tests that directly depend on the given metadata (reverse dependencies).
    /// </summary>
    public IReadOnlyList<TestMetadata> GetDependents(TestMetadata metadata)
    {
        return _dependentMap.TryGetValue(metadata, out var dependents)
            ? dependents
            : Array.Empty<TestMetadata>();
    }
}
