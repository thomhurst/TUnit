using System.Collections.Concurrent;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to the shared data storage for test-scoped values.
/// Accessed via <see cref="TestContext.Data"/>.
/// </summary>
public interface ITestData
{
    /// <summary>
    /// Gets the thread-safe dictionary for storing arbitrary test-scoped data.
    /// Use this to share data between hooks, data sources, and test methods.
    /// Thread-safe for concurrent access.
    /// </summary>
    ConcurrentDictionary<string, object?> Bag { get; }
}
