using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Services;
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
    private readonly GetOnlyDictionary<string, Task<ExecutionContext>> _beforeAssemblyTasks = new();
    private readonly GetOnlyDictionary<Type, Task<ExecutionContext>> _beforeClassTasks = new();

    // Track active test counts for cleanup
    private readonly ConcurrentDictionary<string, int> _assemblyTestCounts = new();
    private readonly ConcurrentDictionary<Type, int> _classTestCounts = new();

    public HookOrchestrator(IHookCollectionService hookCollectionService, TUnitFrameworkLogger logger, IContextProvider contextProvider, TUnitServiceProvider serviceProvider)
    {
        _hookCollectionService = hookCollectionService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _contextProvider = contextProvider;
    }

    public IContextProvider GetContextProvider() => _contextProvider;

    /// <summary>
    /// Gets or creates a cached task for BeforeAssembly hooks.
    /// This ensures the hooks only run once and all tests await the same result.
    /// </summary>
    private Task<ExecutionContext> GetOrCreateBeforeAssemblyTask(string assemblyName, Assembly assembly, CancellationToken cancellationToken)
    {
        return _beforeAssemblyTasks.GetOrAdd(assemblyName, _ => 
            ExecuteBeforeAssemblyHooksAsync(assembly, cancellationToken));
    }

    /// <summary>
    /// Gets or creates a cached task for BeforeClass hooks.
    /// This ensures the hooks only run once and all tests await the same result.
    /// </summary>
    private Task<ExecutionContext> GetOrCreateBeforeClassTask(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClassType, Assembly assembly, CancellationToken cancellationToken)
    {
        return _beforeClassTasks.GetOrAdd(testClassType, async _ =>
        {
#if NET
            var assemblyName = assembly.GetName().Name ?? "Unknown";
            var assemblyContext = await GetOrCreateBeforeAssemblyTask(assemblyName, assembly, cancellationToken);
            ExecutionContext.Restore(assemblyContext);
#endif
            // Now run class hooks in the assembly context
            return await ExecuteBeforeClassHooksAsync(testClassType, cancellationToken);
        });
    }

    public async Task<ExecutionContext?> ExecuteBeforeTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestSessionHooksAsync();

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestSessionContext.RestoreExecutionContext();
                await hook(_contextProvider.TestSessionContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeTestSession hook failed: {ex.Message}");
                throw; // Before hooks should prevent execution on failure
            }
        }

#if NET
        return ExecutionContext.Capture();
#else
        return null;
#endif
    }

    public async Task<ExecutionContext?> ExecuteAfterTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterTestSessionHooksAsync();

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestSessionContext.RestoreExecutionContext();
                await hook(_contextProvider.TestSessionContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterTestSession hook failed: {ex.Message}");
                // After hooks failures are logged but don't stop execution
            }
        }

#if NET
        return ExecutionContext.Capture();
#else
        return null;
#endif
    }

    public async Task<ExecutionContext?> ExecuteBeforeTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestDiscoveryHooksAsync();

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.BeforeTestDiscoveryContext.RestoreExecutionContext();
                await hook(_contextProvider.BeforeTestDiscoveryContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeTestDiscovery hook failed: {ex.Message}");
                throw;
            }
        }

#if NET
        return ExecutionContext.Capture();
#else
        return null;
#endif
    }

    public async Task<ExecutionContext?> ExecuteAfterTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterTestDiscoveryHooksAsync();

        foreach (var hook in hooks)
        {
            try
            {
                _contextProvider.TestDiscoveryContext.RestoreExecutionContext();
                await hook(_contextProvider.TestDiscoveryContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterTestDiscovery hook failed: {ex.Message}");
            }
        }

#if NET
        return ExecutionContext.Capture();
#else
        return null;
#endif
    }

    public async Task<ExecutionContext> OnTestStartingAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        var testClassType = test.Metadata.TestClassType;
        var assemblyName = testClassType.Assembly.GetName().Name ?? "Unknown";

        // Track test counts
        _assemblyTestCounts.AddOrUpdate(assemblyName, 1, (_, count) => count + 1);
        _classTestCounts.AddOrUpdate(testClassType, 1, (_, count) => count + 1);

        // Get or create the BeforeAssembly task - this ensures it only runs once
        await GetOrCreateBeforeAssemblyTask(assemblyName, testClassType.Assembly, cancellationToken);

        // Get or create the BeforeClass task - this ensures it only runs once (and includes assembly context)
        var classContext = await GetOrCreateBeforeClassTask(testClassType, testClassType.Assembly, cancellationToken);

#if NET
        // Restore the class context (which includes assembly context) before running test hooks
        ExecutionContext.Restore(classContext);
#endif

        // Add test to class context if it exists and hasn't been added already
        var classContextObject = _contextProvider.GetOrCreateClassContext(testClassType);

        // Check if test is already in the context (from InitializeContextsWithTestsAsync)
        if (!classContextObject.Tests.Contains(test.Context))
        {
            classContextObject.AddTest(test.Context);
        }

        // Execute BeforeEveryTest hooks in the accumulated context
        await ExecuteBeforeEveryTestHooksAsync(testClassType, test.Context, cancellationToken);

        return ExecutionContext.Capture()!;
    }

    public async Task OnTestCompletedAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        var testClassType = test.Metadata.TestClassType;
        var assemblyName = testClassType.Assembly.GetName().Name ?? "Unknown";

        // Execute AfterEveryTest hooks
        await ExecuteAfterEveryTestHooksAsync(testClassType, test.Context, cancellationToken);

        // Decrement test counts
        var classTestsRemaining = _classTestCounts.AddOrUpdate(testClassType, 0, (_, count) => count - 1);
        var assemblyTestsRemaining = _assemblyTestCounts.AddOrUpdate(assemblyName, 0, (_, count) => count - 1);

        // Execute AfterClass hooks if last test in class
        if (classTestsRemaining == 0)
        {
            await ExecuteAfterClassHooksAsync(testClassType, cancellationToken);
            _classTestCounts.TryRemove(testClassType, out _);
        }

        // Execute AfterAssembly hooks if last test in assembly
        if (assemblyTestsRemaining == 0)
        {
            await ExecuteAfterAssemblyHooksAsync(test.Context.ClassContext.AssemblyContext.Assembly, cancellationToken);
            _assemblyTestCounts.TryRemove(assemblyName, out _);
        }
    }

    private async Task<ExecutionContext> ExecuteBeforeAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeAssemblyHooksAsync(assembly);

        var assemblyContext = _contextProvider.GetOrCreateAssemblyContext(assembly);

        foreach (var hook in hooks)
        {
            try
            {
                assemblyContext.RestoreExecutionContext();
                await hook(assemblyContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeAssembly hook failed for {assembly}: {ex.Message}");
                throw;
            }
        }

        return ExecutionContext.Capture()!;
    }

    private async Task<ExecutionContext> ExecuteAfterAssemblyHooksAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterAssemblyHooksAsync(assembly);

        var assemblyContext = _contextProvider.GetOrCreateAssemblyContext(assembly);

        foreach (var hook in hooks)
        {
            try
            {
                assemblyContext.RestoreExecutionContext();
                await hook(assemblyContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterAssembly hook failed for {assembly.GetName().Name}: {ex.Message}");
            }
        }

        return ExecutionContext.Capture()!;
    }

    private async Task<ExecutionContext> ExecuteBeforeClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClassType, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeClassHooksAsync(testClassType);

        var classContext = _contextProvider.GetOrCreateClassContext(testClassType);

        foreach (var hook in hooks)
        {
            try
            {
                classContext.RestoreExecutionContext();
                await hook(classContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeClass hook failed for {testClassType.Name}: {ex.Message}");
                throw;
            }
        }

        return ExecutionContext.Capture()!;
    }

    private async Task<ExecutionContext> ExecuteAfterClassHooksAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClassType, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterClassHooksAsync(testClassType);

        var classContext = _contextProvider.GetOrCreateClassContext(testClassType);

        foreach (var hook in hooks)
        {
            try
            {
                classContext.RestoreExecutionContext();
                await hook(classContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterClass hook failed for {testClassType.Name}: {ex.Message}");
            }
        }

        return ExecutionContext.Capture()!;
    }

    private async Task ExecuteBeforeEveryTestHooksAsync(Type testClassType, TestContext testContext, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeEveryTestHooksAsync(testClassType);

        foreach (var hook in hooks)
        {
            try
            {
                testContext.RestoreExecutionContext();
                await hook(testContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeEveryTest hook failed: {ex.Message}");
                throw;
            }
        }
    }

    private async Task ExecuteAfterEveryTestHooksAsync(Type testClassType, TestContext testContext, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterEveryTestHooksAsync(testClassType);

        foreach (var hook in hooks)
        {
            try
            {
                testContext.RestoreExecutionContext();
                await hook(testContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterEveryTest hook failed: {ex.Message}");
            }
        }
    }
}
