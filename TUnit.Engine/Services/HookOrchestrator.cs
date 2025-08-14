using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Helpers;
using TUnit.Core.Services;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Framework;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal sealed class HookOrchestrator
{
    private readonly IHookCollectionService _hookCollectionService;
    private readonly TUnitFrameworkLogger _logger;
    private readonly TUnitServiceProvider _serviceProvider;
    private readonly IContextProvider _contextProvider;

    // Cache initialization tasks for assemblies/classes
    private readonly GetOnlyDictionary<string, Task<ExecutionContext?>> _beforeAssemblyTasks = new();
    private readonly GetOnlyDictionary<Type, Task<ExecutionContext?>> _beforeClassTasks = new();

    // Track active test counts for cleanup
    private readonly ConcurrentDictionary<string, Counter> _assemblyTestCounts = new();
    private readonly ConcurrentDictionary<Type, Counter> _classTestCounts = new();
    
    // Cache whether hooks exist to avoid unnecessary collection
    private readonly GetOnlyDictionary<Type, Task<bool>> _hasBeforeEveryTestHooks = new();
    private readonly GetOnlyDictionary<Type, Task<bool>> _hasAfterEveryTestHooks = new();
    
    // Store session context to flow to assembly/class hooks
#if NET
    private ExecutionContext? _sessionExecutionContext;
#endif

    public HookOrchestrator(IHookCollectionService hookCollectionService, TUnitFrameworkLogger logger, IContextProvider contextProvider, TUnitServiceProvider serviceProvider)
    {
        _hookCollectionService = hookCollectionService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _contextProvider = contextProvider;
    }

    public IContextProvider GetContextProvider() => _contextProvider;
    
    public IHookCollectionService HookCollectionService => _hookCollectionService;

    /// <summary>
    /// Pre-registers all tests so we know the total count per class/assembly
    /// This must be called before any tests start executing
    /// </summary>
    public void RegisterTests(IEnumerable<AbstractExecutableTest> tests)
    {
        // Group tests by class and assembly to get counts
        var testsByClass = tests.GroupBy(t => t.Metadata.TestClassType);
        var testsByAssembly = tests.GroupBy(t => t.Metadata.TestClassType.Assembly.GetName().Name ?? "Unknown");
        
        // Initialize counters with the total count for each class
        foreach (var classGroup in testsByClass)
        {
            var classType = classGroup.Key;
            var count = classGroup.Count();
            _classTestCounts.GetOrAdd(classType, _ => new Counter()).Add(count);
        }
        
        // Initialize counters with the total count for each assembly  
        foreach (var assemblyGroup in testsByAssembly)
        {
            var assemblyName = assemblyGroup.Key;
            var count = assemblyGroup.Count();
            _assemblyTestCounts.GetOrAdd(assemblyName, _ => new Counter()).Add(count);
        }
    }

    /// <summary>
    /// Gets or creates a cached task for BeforeAssembly hooks.
    /// This ensures the hooks only run once and all tests await the same result.
    /// </summary>
    private Task<ExecutionContext?> GetOrCreateBeforeAssemblyTask(string assemblyName, Assembly assembly, CancellationToken cancellationToken)
    {
        return _beforeAssemblyTasks.GetOrAdd(assemblyName, _ =>
            ExecuteBeforeAssemblyHooksAsync(assembly, cancellationToken));
    }

    /// <summary>
    /// Gets or creates a cached task for BeforeClass hooks.
    /// This ensures the hooks only run once and all tests await the same result.
    /// </summary>
    private Task<ExecutionContext?> GetOrCreateBeforeClassTask(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClassType, Assembly assembly, CancellationToken cancellationToken)
    {
        return _beforeClassTasks.GetOrAdd(testClassType, async _ =>
        {
#if NET
            var assemblyName = assembly.GetName().Name ?? "Unknown";
            var assemblyContext = await GetOrCreateBeforeAssemblyTask(assemblyName, assembly, cancellationToken).ConfigureAwait(false);
            if (assemblyContext != null)
            {
                ExecutionContext.Restore(assemblyContext);
            }
#endif
            // Now run class hooks in the assembly context
            return await ExecuteBeforeClassHooksAsync(testClassType, cancellationToken).ConfigureAwait(false);
        });
    }

    public async Task<ExecutionContext?> ExecuteBeforeTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestSessionHooksAsync().ConfigureAwait(false);

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestSessionContext.RestoreExecutionContext();
                await hook(_contextProvider.TestSessionContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeTestSession hook failed: {ex.Message}").ConfigureAwait(false);
                throw; // Before hooks should prevent execution on failure
            }
        }

#if NET
        // Store and return the context's ExecutionContext if user called AddAsyncLocalValues
        _sessionExecutionContext = _contextProvider.TestSessionContext.ExecutionContext;
        return _sessionExecutionContext;
#else
        return null;
#endif
    }

    public async Task<ExecutionContext?> ExecuteAfterTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterTestSessionHooksAsync().ConfigureAwait(false);
        var exceptions = new List<Exception>();

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestSessionContext.RestoreExecutionContext();
                await hook(_contextProvider.TestSessionContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterTestSession hook failed: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw exceptions.Count == 1
                ? new HookFailedException(exceptions[0])
                : new HookFailedException("Multiple AfterTestSession hooks failed", new AggregateException(exceptions));
        }

#if NET
        // Return the context's ExecutionContext if user called AddAsyncLocalValues
        return _contextProvider.TestSessionContext.ExecutionContext;
#else
        return null;
#endif
    }

    public async Task<ExecutionContext?> ExecuteBeforeTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestDiscoveryHooksAsync().ConfigureAwait(false);

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.BeforeTestDiscoveryContext.RestoreExecutionContext();
                await hook(_contextProvider.BeforeTestDiscoveryContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeTestDiscovery hook failed: {ex.Message}").ConfigureAwait(false);
                throw;
            }
        }

#if NET
        // Return the context's ExecutionContext if user called AddAsyncLocalValues
        return _contextProvider.BeforeTestDiscoveryContext.ExecutionContext;
#else
        return null;
#endif
    }

    public async Task<ExecutionContext?> ExecuteAfterTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterTestDiscoveryHooksAsync().ConfigureAwait(false);
        var exceptions = new List<Exception>();

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestDiscoveryContext.RestoreExecutionContext();
                await hook(_contextProvider.TestDiscoveryContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterTestDiscovery hook failed: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw exceptions.Count == 1
                ? new HookFailedException(exceptions[0])
                : new HookFailedException("Multiple AfterTestDiscovery hooks failed", new AggregateException(exceptions));
        }

#if NET
        // Return the context's ExecutionContext if user called AddAsyncLocalValues
        return _contextProvider.TestDiscoveryContext.ExecutionContext;
#else
        return null;
#endif
    }

    public async Task<ExecutionContext?> OnTestStartingAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        if (test.Context.TestDetails.ClassInstance is SkippedTestInstance)
        {
            return null;
        }

        var testClassType = test.Metadata.TestClassType;
        var assemblyName = testClassType.Assembly.GetName().Name ?? "Unknown";

        // Note: Test counts are pre-registered in RegisterTests(), no increment here

        // Fast path: check if we need to run hooks at all
        var hasHooks = await _hasBeforeEveryTestHooks.GetOrAdd(testClassType, async _ => 
        {
            var hooks = await _hookCollectionService.CollectBeforeEveryTestHooksAsync(testClassType).ConfigureAwait(false);
            return hooks.Count > 0;
        });

        await GetOrCreateBeforeAssemblyTask(assemblyName, testClassType.Assembly, cancellationToken).ConfigureAwait(false);

        // Get the cached class context (includes assembly context)
        var classContext = await GetOrCreateBeforeClassTask(testClassType, testClassType.Assembly, cancellationToken).ConfigureAwait(false);

#if NET
        // Batch context restoration - only restore once if we have hooks to run
        if (classContext != null && hasHooks)
        {
            ExecutionContext.Restore(classContext);
        }
#endif

        var classContextObject = _contextProvider.GetOrCreateClassContext(testClassType);

        // Execute BeforeEveryTest hooks only if they exist
        if (hasHooks)
        {
            await ExecuteBeforeEveryTestHooksAsync(testClassType, test.Context, cancellationToken).ConfigureAwait(false);
        }

        // Return whichever context has AsyncLocal values:
        // 1. If test context has it (from BeforeTest hooks), use that
        // 2. Otherwise, use class context if it has it
        // 3. Otherwise null (no AsyncLocal values to flow)
#if NET
        return test.Context.ExecutionContext ?? classContext;
#else
        return null;
#endif
    }

    public async Task OnTestCompletedAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        if (test.Context.TestDetails.ClassInstance is SkippedTestInstance)
        {
            return;
        }

        var testClassType = test.Metadata.TestClassType;
        var assemblyName = testClassType.Assembly.GetName().Name ?? "Unknown";
        var exceptions = new List<Exception>();

        // Fast path: check if we have hooks to execute
        var hasHooks = await _hasAfterEveryTestHooks.GetOrAdd(testClassType, async _ =>
        {
            var hooks = await _hookCollectionService.CollectAfterEveryTestHooksAsync(testClassType).ConfigureAwait(false);
            return hooks.Count > 0;
        });

        // Execute AfterEveryTest hooks only if they exist
        if (hasHooks)
        {
            try
            {
                await ExecuteAfterEveryTestHooksAsync(testClassType, test.Context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterEveryTest hooks failed: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
        }

        // ALWAYS decrement test counts, even if hooks failed
        var classCounter = _classTestCounts.GetOrAdd(testClassType, _ => new Counter());
        var classTestsRemaining = classCounter.Decrement();
        var assemblyCounter = _assemblyTestCounts.GetOrAdd(assemblyName, _ => new Counter());
        var assemblyTestsRemaining = assemblyCounter.Decrement();

        // Execute AfterClass hooks if last test in class AND BeforeClass hooks were run
        if (classTestsRemaining == 0 && _beforeClassTasks.TryGetValue(testClassType, out _))
        {
            try
            {
                await ExecuteAfterClassHooksAsync(testClassType, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterClass hooks failed: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
            finally
            {
                // Always remove from dictionary to prevent memory leaks
                _classTestCounts.TryRemove(testClassType, out _);
            }
        }

        // Execute AfterAssembly hooks if last test in assembly AND BeforeAssembly hooks were run
        if (assemblyTestsRemaining == 0 && _beforeAssemblyTasks.TryGetValue(assemblyName, out _))
        {
            try
            {
                await ExecuteAfterAssemblyHooksAsync(test.Context.ClassContext.AssemblyContext.Assembly, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterAssembly hooks failed: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
            finally
            {
                // Always remove from dictionary to prevent memory leaks
                _assemblyTestCounts.TryRemove(assemblyName, out _);
            }
        }

        // If any hooks failed, throw an aggregate exception
        if (exceptions.Count > 0)
        {
            throw exceptions.Count == 1
                ? new HookFailedException(exceptions[0])
                : new HookFailedException("Multiple hook levels failed during test cleanup", new AggregateException(exceptions));
        }
    }

    private async Task<ExecutionContext?> ExecuteBeforeAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeAssemblyHooksAsync(assembly).ConfigureAwait(false);

        var assemblyContext = _contextProvider.GetOrCreateAssemblyContext(assembly);

#if NET
        // Restore session context first if it exists (to flow TestSession -> Assembly)
        if (_sessionExecutionContext != null)
        {
            ExecutionContext.Restore(_sessionExecutionContext);
        }
#endif

        foreach (var hook in hooks)
        {
            try
            {
                assemblyContext.RestoreExecutionContext();
                await hook(assemblyContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeAssembly hook failed for {assembly}: {ex.Message}").ConfigureAwait(false);
                throw;
            }
        }

        // Execute global BeforeEveryAssembly hooks
        var everyHooks = await _hookCollectionService.CollectBeforeEveryAssemblyHooksAsync().ConfigureAwait(false);
        foreach (var hook in everyHooks)
        {
            try
            {
                assemblyContext.RestoreExecutionContext();
                await hook(assemblyContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeEveryAssembly hook failed for {assembly}: {ex.Message}").ConfigureAwait(false);
                throw;
            }
        }

        // Return the context's ExecutionContext if user called AddAsyncLocalValues, otherwise null
#if NET
        return assemblyContext.ExecutionContext;
#else
        return null;
#endif
    }

    private async Task<ExecutionContext?> ExecuteAfterAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterAssemblyHooksAsync(assembly).ConfigureAwait(false);
        var assemblyContext = _contextProvider.GetOrCreateAssemblyContext(assembly);
        var exceptions = new List<Exception>();

        // Execute global AfterEveryAssembly hooks first
        var everyHooks = await _hookCollectionService.CollectAfterEveryAssemblyHooksAsync().ConfigureAwait(false);
        foreach (var hook in everyHooks)
        {
            try
            {
                assemblyContext.RestoreExecutionContext();
                await hook(assemblyContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterEveryAssembly hook failed for {assembly.GetName().Name}: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
        }

        foreach (var hook in hooks)
        {
            try
            {
                assemblyContext.RestoreExecutionContext();
                await hook(assemblyContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterAssembly hook failed for {assembly.GetName().Name}: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw exceptions.Count == 1
                ? new HookFailedException(exceptions[0])
                : new HookFailedException("Multiple AfterAssembly hooks failed", new AggregateException(exceptions));
        }

        // Return the context's ExecutionContext if user called AddAsyncLocalValues, otherwise null
#if NET
        return assemblyContext.ExecutionContext;
#else
        return null;
#endif
    }

    private async Task<ExecutionContext?> ExecuteBeforeClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClassType, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeClassHooksAsync(testClassType).ConfigureAwait(false);

        var classContext = _contextProvider.GetOrCreateClassContext(testClassType);

        foreach (var hook in hooks)
        {
            try
            {
                classContext.RestoreExecutionContext();
                await hook(classContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeClass hook failed for {testClassType.Name}: {ex.Message}").ConfigureAwait(false);
                throw;
            }
        }

        // Execute global BeforeEveryClass hooks
        var everyHooks = await _hookCollectionService.CollectBeforeEveryClassHooksAsync().ConfigureAwait(false);
        foreach (var hook in everyHooks)
        {
            try
            {
                classContext.RestoreExecutionContext();
                await hook(classContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeEveryClass hook failed for {testClassType.Name}: {ex.Message}").ConfigureAwait(false);
                throw;
            }
        }

        // Return the context's ExecutionContext if user called AddAsyncLocalValues, otherwise null
#if NET
        return classContext.ExecutionContext;
#else
        return null;
#endif
    }

    private async Task<ExecutionContext?> ExecuteAfterClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClassType, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterClassHooksAsync(testClassType).ConfigureAwait(false);
        var classContext = _contextProvider.GetOrCreateClassContext(testClassType);
        var exceptions = new List<Exception>();

        // Execute global AfterEveryClass hooks first
        var everyHooks = await _hookCollectionService.CollectAfterEveryClassHooksAsync().ConfigureAwait(false);
        foreach (var hook in everyHooks)
        {
            try
            {
                classContext.RestoreExecutionContext();
                await hook(classContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterEveryClass hook failed for {testClassType.Name}: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
        }

        foreach (var hook in hooks)
        {
            try
            {
                classContext.RestoreExecutionContext();
                await hook(classContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterClass hook failed for {testClassType.Name}: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw exceptions.Count == 1
                ? new HookFailedException(exceptions[0])
                : new HookFailedException("Multiple AfterClass hooks failed", new AggregateException(exceptions));
        }

        // Return the context's ExecutionContext if user called AddAsyncLocalValues, otherwise null
#if NET
        return classContext.ExecutionContext;
#else
        return null;
#endif
    }

    private async Task ExecuteBeforeEveryTestHooksAsync(Type testClassType, TestContext testContext, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeEveryTestHooksAsync(testClassType).ConfigureAwait(false);

        foreach (var hook in hooks)
        {
            try
            {
                testContext.RestoreExecutionContext();
                await hook(testContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeEveryTest hook failed: {ex.Message}").ConfigureAwait(false);
                throw;
            }
        }
    }

    private async Task ExecuteAfterEveryTestHooksAsync(Type testClassType, TestContext testContext, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterEveryTestHooksAsync(testClassType).ConfigureAwait(false);
        var exceptions = new List<Exception>();

        foreach (var hook in hooks)
        {
            try
            {
                testContext.RestoreExecutionContext();
                await hook(testContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterEveryTest hook failed: {ex.Message}").ConfigureAwait(false);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw exceptions.Count == 1
                ? new HookFailedException(exceptions[0])
                : new HookFailedException("Multiple AfterEveryTest hooks failed", new AggregateException(exceptions));
        }
    }
}
