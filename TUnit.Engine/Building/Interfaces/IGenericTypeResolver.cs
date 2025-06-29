using TUnit.Core;

namespace TUnit.Engine.Building.Interfaces;

/// <summary>
/// Interface for resolving generic types in test metadata
/// </summary>
public interface IGenericTypeResolver
{
    /// <summary>
    /// Resolves generic types for all test metadata
    /// </summary>
    /// <param name="metadata">Collection of test metadata that may contain generic types</param>
    /// <returns>Collection of test metadata with resolved generic types</returns>
    Task<IEnumerable<TestMetadata>> ResolveGenericsAsync(IEnumerable<TestMetadata> metadata);
}
