using System.Reflection;

namespace TUnit.Core.Services;

/// <summary>
/// Interface for building and managing context hierarchies
/// </summary>
public interface IContextBuilder
{
    /// <summary>
    /// Gets or creates the discovery context
    /// </summary>
    BeforeTestDiscoveryContext GetOrCreateDiscoveryContext();

    /// <summary>
    /// Gets or creates the test discovery context
    /// </summary>
    TestDiscoveryContext GetOrCreateTestDiscoveryContext();

    /// <summary>
    /// Gets or creates a test session context
    /// </summary>
    TestSessionContext GetOrCreateSessionContext(string sessionId, string? testFilter = null);

    /// <summary>
    /// Gets or creates an assembly context
    /// </summary>
    AssemblyHookContext GetOrCreateAssemblyContext(Assembly assembly, TestSessionContext? sessionContext = null);

    /// <summary>
    /// Gets or creates a class context
    /// </summary>
    ClassHookContext GetOrCreateClassContext(Type classType, AssemblyHookContext? assemblyContext = null);

    /// <summary>
    /// Creates a test context with proper parent hierarchy
    /// </summary>
    TestContext CreateTestContext(
        string testName,
        Type classType,
        CancellationToken cancellationToken,
        IServiceProvider serviceProvider,
        ClassHookContext? classContext = null);

    /// <summary>
    /// Creates the full context hierarchy for a test
    /// </summary>
    TestContext CreateFullTestContextHierarchy(
        string testName,
        Type classType,
        string? sessionId = null,
        string? testFilter = null,
        CancellationToken cancellationToken = default,
        IServiceProvider? serviceProvider = null);

    /// <summary>
    /// Gets the existing class context for a type if it exists
    /// </summary>
    ClassHookContext? GetClassContext(Type classType);

    /// <summary>
    /// Gets the existing assembly context for an assembly if it exists
    /// </summary>
    AssemblyHookContext? GetAssemblyContext(Assembly assembly);

    /// <summary>
    /// Gets the existing session context for a session ID if it exists
    /// </summary>
    TestSessionContext? GetSessionContext(string sessionId);

    /// <summary>
    /// Clears all cached contexts
    /// </summary>
    void Clear();
}