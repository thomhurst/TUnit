using TUnit.Core;

namespace TUnit.Engine.Building.Interfaces;

/// <summary>
/// Interface for collecting test metadata from various sources (AOT or reflection)
/// </summary>
public interface ITestDataCollector
{
    /// <summary>
    /// Collects all test metadata from the configured source
    /// </summary>
    /// <returns>Collection of test metadata ready for processing</returns>
    Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId);
}
