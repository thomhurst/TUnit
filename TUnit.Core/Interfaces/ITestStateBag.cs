using System.Collections.Concurrent;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to the runtime state storage for test-scoped values.
/// Accessed via <see cref="TestContext.StateBag"/>.
/// This is a thread-safe key-value store for sharing data between hooks, data sources, and test methods.
/// </summary>
public interface ITestStateBag
{
    /// <summary>
    /// Gets the thread-safe dictionary for storing arbitrary test-scoped data.
    /// Use this to share state between hooks, data sources, and test methods within a single test execution.
    /// Thread-safe for concurrent access.
    /// </summary>
    ConcurrentDictionary<string, object?> Bag { get; }
}
