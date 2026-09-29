using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Services;

namespace TUnit.Core;

/// <summary>
/// Builder for creating and managing the context hierarchy with proper parent-child relationships and singleton behavior
/// </summary>
internal class ContextProvider(IServiceProvider serviceProvider, string testSessionId, string? testFilter) : IContextProvider
{
    private readonly ConcurrentDictionary<Assembly, AssemblyHookContext> _assemblyContexts = new();
    private readonly ConcurrentDictionary<Type, ClassHookContext> _classContexts = new();
    private readonly Lock _creationLock = new();

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
        if (_assemblyContexts.TryGetValue(assembly, out var existing))
        {
            return existing;
        }

        // Not ConcurrentDictionary.GetOrAdd: the context constructor registers itself with its
        // parent, so a factory that loses the GetOrAdd race would leave an orphaned, test-less
        // context visible through TestSessionContext.Assemblies. Creation happens once per
        // assembly, so the lock is off the hot path.
        lock (_creationLock)
        {
            if (_assemblyContexts.TryGetValue(assembly, out existing))
            {
                return existing;
            }

            var created = new AssemblyHookContext(TestSessionContext)
            {
                Assembly = assembly
            };

            _assemblyContexts[assembly] = created;
            return created;
        }
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
        if (_classContexts.TryGetValue(classType, out var existing))
        {
            return existing;
        }

        // See GetOrCreateAssemblyContext: a lost GetOrAdd race would leave an orphaned class
        // context in AssemblyHookContext.TestClasses. The assembly context is resolved before
        // taking _creationLock on purpose, so the assembly and class paths never nest the lock.
        var assemblyContext = GetOrCreateAssemblyContext(classType.Assembly);

        lock (_creationLock)
        {
            if (_classContexts.TryGetValue(classType, out existing))
            {
                return existing;
            }

            var created = new ClassHookContext(assemblyContext)
            {
                ClassType = classType
            };

            _classContexts[classType] = created;
            return created;
        }
    }

    /// <summary>
    /// Creates a test context with proper parent hierarchy
    /// </summary>
    public TestContext CreateTestContext(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classType,
        TestBuilderContext testBuilderContext,
        TestDetails testDetails,
        CancellationToken cancellationToken)
    {
        var classContext = GetOrCreateClassContext(classType);

        var testContext = new TestContext(testDetails.TestName, serviceProvider, classContext, testBuilderContext, cancellationToken)
        {
            // Must be assigned before AddTest publishes the context via ClassHookContext.Tests —
            // AfterEvery(Class) hooks can iterate Tests while sibling dynamic tests are still being built.
            TestDetails = testDetails,
        };

        classContext.AddTest(testContext);

        return testContext;
    }
}
