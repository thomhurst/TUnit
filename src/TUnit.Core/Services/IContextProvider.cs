using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Services;

/// <summary>
/// Interface for building and managing context hierarchies
/// </summary>
internal interface IContextProvider
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
    ClassHookContext GetOrCreateClassContext(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classType);

    /// <summary>
    /// Creates a test context. <paramref name="testDetails"/> is assigned before the context
    /// becomes observable via <see cref="ClassHookContext.Tests"/> so hooks never see a
    /// partially-built context. Cleanup-only contexts pass <paramref name="registerAsTest"/> as false
    /// to stay out of the class and session test lists.
    /// </summary>
    TestContext CreateTestContext(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classType,
        TestBuilderContext testBuilderContext,
        TestDetails testDetails,
        CancellationToken cancellationToken,
        bool registerAsTest = true);
}
