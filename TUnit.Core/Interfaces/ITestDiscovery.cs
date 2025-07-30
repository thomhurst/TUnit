using System.Collections.Generic;
using System.Reflection;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Base interface for test discovery implementations.
/// </summary>
public interface ITestDiscovery
{
    /// <summary>
    /// Gets whether this discovery implementation supports AOT compilation.
    /// </summary>
    bool SupportsAot { get; }
    
    /// <summary>
    /// Discovers tests in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for tests.</param>
    /// <returns>The discovered test metadata.</returns>
    IEnumerable<TestMetadata> DiscoverTests(Assembly assembly);
}