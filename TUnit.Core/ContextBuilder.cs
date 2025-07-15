using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Services;

namespace TUnit.Core;

/// <summary>
/// Builder for creating and managing the context hierarchy with proper parent-child relationships and singleton behavior
/// </summary>
public class ContextBuilder : IContextBuilder
{
    private readonly ConcurrentDictionary<string, TestSessionContext> _sessionContexts = new();
    private readonly ConcurrentDictionary<Assembly, AssemblyHookContext> _assemblyContexts = new();
    private readonly ConcurrentDictionary<Type, ClassHookContext> _classContexts = new();
    
    private BeforeTestDiscoveryContext? _discoveryContext;
    private TestDiscoveryContext? _testDiscoveryContext;

    /// <summary>
    /// Gets or creates the discovery context
    /// </summary>
    public BeforeTestDiscoveryContext GetOrCreateDiscoveryContext()
    {
        if (_discoveryContext == null)
        {
            _discoveryContext = new BeforeTestDiscoveryContext
            {
                TestFilter = string.Empty
            };
        }
        return _discoveryContext;
    }

    /// <summary>
    /// Gets or creates the test discovery context
    /// </summary>
    public TestDiscoveryContext GetOrCreateTestDiscoveryContext()
    {
        if (_testDiscoveryContext == null)
        {
            var discoveryContext = GetOrCreateDiscoveryContext();
            _testDiscoveryContext = new TestDiscoveryContext(discoveryContext)
            {
                TestFilter = null
            };
        }
        return _testDiscoveryContext;
    }

    /// <summary>
    /// Gets or creates a test session context
    /// </summary>
    public TestSessionContext GetOrCreateSessionContext(string sessionId, string? testFilter = null)
    {
        return _sessionContexts.GetOrAdd(sessionId, id =>
        {
            var testDiscoveryContext = GetOrCreateTestDiscoveryContext();
            return new TestSessionContext(testDiscoveryContext)
            {
                Id = id,
                TestFilter = testFilter
            };
        });
    }

    /// <summary>
    /// Gets or creates an assembly context
    /// </summary>
    public AssemblyHookContext GetOrCreateAssemblyContext(Assembly assembly, TestSessionContext? sessionContext = null)
    {
        // If no session context provided, use or create a default one
        sessionContext ??= GetOrCreateSessionContext("default-session");

        return _assemblyContexts.GetOrAdd(assembly, asm =>
        {
            return new AssemblyHookContext(sessionContext)
            {
                Assembly = asm
            };
        });
    }

    /// <summary>
    /// Gets or creates a class context
    /// </summary>
    public ClassHookContext GetOrCreateClassContext(Type classType, AssemblyHookContext? assemblyContext = null)
    {
        // If no assembly context provided, create one from the class type's assembly
        assemblyContext ??= GetOrCreateAssemblyContext(classType.Assembly);

        return _classContexts.GetOrAdd(classType, type =>
        {
            return new ClassHookContext(assemblyContext)
            {
                ClassType = type
            };
        });
    }

    /// <summary>
    /// Creates a test context with proper parent hierarchy
    /// </summary>
    public TestContext CreateTestContext(
        string testName, 
        Type classType,
        CancellationToken cancellationToken,
        IServiceProvider serviceProvider,
        ClassHookContext? classContext = null)
    {
        // If no class context provided, create the full hierarchy
        classContext ??= GetOrCreateClassContext(classType);

        var testContext = new TestContext(testName, cancellationToken, serviceProvider, classContext);
        
        // Add the test to its class context
        classContext.AddTest(testContext);
        
        return testContext;
    }

    /// <summary>
    /// Creates the full context hierarchy for a test
    /// </summary>
    public TestContext CreateFullTestContextHierarchy(
        string testName,
        Type classType,
        string? sessionId = null,
        string? testFilter = null,
        CancellationToken cancellationToken = default,
        IServiceProvider? serviceProvider = null)
    {
        // Create or get the session context
        var sessionContext = GetOrCreateSessionContext(sessionId ?? Guid.NewGuid().ToString(), testFilter);
        
        // Create or get the assembly context
        var assemblyContext = GetOrCreateAssemblyContext(classType.Assembly, sessionContext);
        
        // Create or get the class context
        var classContext = GetOrCreateClassContext(classType, assemblyContext);
        
        // Create the test context
        serviceProvider ??= new TestServiceProvider();
        return CreateTestContext(testName, classType, cancellationToken, serviceProvider, classContext);
    }

    /// <summary>
    /// Gets the existing class context for a type if it exists
    /// </summary>
    public ClassHookContext? GetClassContext(Type classType)
    {
        return _classContexts.TryGetValue(classType, out var context) ? context : null;
    }

    /// <summary>
    /// Gets the existing assembly context for an assembly if it exists
    /// </summary>
    public AssemblyHookContext? GetAssemblyContext(Assembly assembly)
    {
        return _assemblyContexts.TryGetValue(assembly, out var context) ? context : null;
    }

    /// <summary>
    /// Gets the existing session context for a session ID if it exists
    /// </summary>
    public TestSessionContext? GetSessionContext(string sessionId)
    {
        return _sessionContexts.TryGetValue(sessionId, out var context) ? context : null;
    }

    /// <summary>
    /// Clears all cached contexts
    /// </summary>
    public void Clear()
    {
        _sessionContexts.Clear();
        _assemblyContexts.Clear();
        _classContexts.Clear();
        _discoveryContext = null;
        _testDiscoveryContext = null;
    }
}