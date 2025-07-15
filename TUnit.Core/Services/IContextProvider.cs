using System.Reflection;

namespace TUnit.Core.Services;

/// <summary>
/// Interface for building and managing context hierarchies
/// </summary>
public interface IContextProvider
{
    /// <summary>
    /// Gets or creates the discovery context
    /// </summary>
    BeforeTestDiscoveryContext BeforeTestDiscoveryContext { get; }

    /// <summary>
    /// Gets or creates the test discovery context
    /// </summary>
    TestDiscoveryContext TestDiscoveryContext { get; }

    /// <summary>
    /// Gets or creates a test session context
    /// </summary>
    TestSessionContext TestSessionContext { get; }

    /// <summary>
    /// Gets or creates an assembly context
    /// </summary>
    AssemblyHookContext GetOrCreateAssemblyContext(Assembly assembly);

    /// <summary>
    /// Gets or creates a class context
    /// </summary>
    ClassHookContext GetOrCreateClassContext(Type classType);

    /// <summary>
    /// Creates a test context
    /// </summary>
    TestContext CreateTestContext(
        string testName,
        Type classType,
        CancellationToken cancellationToken,
        IServiceProvider serviceProvider);
}
