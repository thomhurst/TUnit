using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Services;

namespace TUnit.Core;

/// <summary>
/// Builder for creating and managing the context hierarchy with proper parent-child relationships and singleton behavior
/// </summary>
public class ContextProvider(IServiceProvider serviceProvider, string testSessionId, string? testFilter) : IContextProvider
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
        return _assemblyContexts.GetOrAdd(assembly, static (assembly, context) =>
            new AssemblyHookContext(context)
            {
                Assembly = assembly
            }, TestSessionContext);
    }

    /// <summary>
    /// Gets or creates a class context
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2111",
        Justification = "Type parameter is annotated at the method boundary.")]
    public ClassHookContext GetOrCreateClassContext(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classType)
    {
        return _classContexts.GetOrAdd(classType, static ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
            type, state) =>
        {
            return new ClassHookContext(state.GetOrCreateAssemblyContext(type.Assembly))
            {
                ClassType = type
            };
        }, this);
    }

    /// <summary>
    /// Creates a test context with proper parent hierarchy
    /// </summary>
    public TestContext CreateTestContext(
        string testName,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classType,
        TestBuilderContext testBuilderContext,
        CancellationToken cancellationToken)
    {
        var classContext = GetOrCreateClassContext(classType);

        var testContext = new TestContext(testName, serviceProvider, classContext, testBuilderContext, cancellationToken);

        classContext.AddTest(testContext);

        return testContext;
    }
}
