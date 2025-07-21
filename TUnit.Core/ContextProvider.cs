using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Services;

namespace TUnit.Core;

/// <summary>
/// Builder for creating and managing the context hierarchy with proper parent-child relationships and singleton behavior
/// </summary>
public class ContextProvider(string testSessionId, string? testFilter) : IContextProvider
{
    private readonly ConcurrentDictionary<Assembly, AssemblyHookContext> _assemblyContexts = new();
    private readonly ConcurrentDictionary<Type, ClassHookContext> _classContexts = new();

    public GlobalContext GlobalContext { get; } = new()
    {
        TestFilter = testFilter,
    };

    /// <summary>
    /// Gets or creates the discovery context
    /// </summary>
    [field: AllowNull, MaybeNull]
    public BeforeTestDiscoveryContext BeforeTestDiscoveryContext => field ??= new BeforeTestDiscoveryContext
    {
        TestFilter = testFilter
    };

    /// <summary>
    /// Gets or creates the test discovery context
    /// </summary>
    [field: AllowNull, MaybeNull]
    public TestDiscoveryContext TestDiscoveryContext => field ??= new TestDiscoveryContext(BeforeTestDiscoveryContext)
    {
        TestFilter = testFilter
    };

    /// <summary>
    /// Gets or creates a test session context
    /// </summary>
    [field: AllowNull, MaybeNull]
    public TestSessionContext TestSessionContext => field ??= new TestSessionContext(TestDiscoveryContext)
    {
        Id = testSessionId,
        TestFilter = testFilter
    };

    /// <summary>
    /// Gets or creates an assembly context
    /// </summary>
    public AssemblyHookContext GetOrCreateAssemblyContext(Assembly assembly)
    {
        return _assemblyContexts.GetOrAdd(assembly, asm =>
        {
            return new AssemblyHookContext(TestSessionContext)
            {
                Assembly = assembly
            };
        });
    }

    /// <summary>
    /// Gets or creates a class context
    /// </summary>
    public ClassHookContext GetOrCreateClassContext(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classType)
    {
        return _classContexts.GetOrAdd(classType, type =>
        {
            return new ClassHookContext(GetOrCreateAssemblyContext(classType.Assembly))
            {
                ClassType = classType
            };
        });
    }

    /// <summary>
    /// Creates a test context with proper parent hierarchy
    /// </summary>
    public TestContext CreateTestContext(
        string testName,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classType,
        CancellationToken cancellationToken,
        IServiceProvider serviceProvider)
    {
        var classContext = GetOrCreateClassContext(classType);

        var testContext = new TestContext(testName, cancellationToken, serviceProvider, classContext);

        // Add the test to its class context
        classContext.AddTest(testContext);

        return testContext;
    }
}
