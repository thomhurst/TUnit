using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Models;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal sealed class HookOrchestrator
{
    private readonly IHookCollectionService _hookCollectionService;
    private readonly TUnitFrameworkLogger _logger;
    
    // Track which assemblies/classes have been initialized
    private readonly ConcurrentDictionary<string, bool> _initializedAssemblies = new();
    private readonly ConcurrentDictionary<Type, bool> _initializedClasses = new();
    
    // Track active test counts for cleanup
    private readonly ConcurrentDictionary<string, int> _assemblyTestCounts = new();
    private readonly ConcurrentDictionary<Type, int> _classTestCounts = new();
    
    // Track contexts
    private readonly ConcurrentDictionary<string, AssemblyHookContext> _assemblyContexts = new();
    private readonly ConcurrentDictionary<Type, ClassHookContext> _classContexts = new();
    
    public HookOrchestrator(IHookCollectionService hookCollectionService, TUnitFrameworkLogger logger)
    {
        _hookCollectionService = hookCollectionService;
        _logger = logger;
    }
    
    private void EnsureContextHierarchy()
    {
        if (BeforeTestDiscoveryContext.Current == null)
        {
            var discoveryContext = new BeforeTestDiscoveryContext { TestFilter = null };
            BeforeTestDiscoveryContext.Current = discoveryContext;
        }
        
        if (TestSessionContext.Current == null)
        {
            var testDiscoveryContext = new TestDiscoveryContext(BeforeTestDiscoveryContext.Current) { TestFilter = null };
            var sessionContext = new TestSessionContext(testDiscoveryContext) 
            { 
                Id = Guid.NewGuid().ToString(), 
                TestFilter = null 
            };
            TestSessionContext.Current = sessionContext;
        }
    }
    
    private int _totalTestCount = 0;
    
    public void SetTotalTestCount(int count)
    {
        _totalTestCount = count;
    }
    
    public Task InitializeContextsWithTestsAsync(IEnumerable<ExecutableTest> tests, CancellationToken cancellationToken)
    {
        // Ensure context hierarchy exists
        EnsureContextHierarchy();
        
        // Group tests by assembly and class
        var testsByAssembly = tests.GroupBy(t => t.Metadata.TestClassType.Assembly.GetName().Name ?? "Unknown");
        
        foreach (var assemblyGroup in testsByAssembly)
        {
            var assemblyName = assemblyGroup.Key;
            
            // Get or create assembly context
            var assemblyContext = _assemblyContexts.GetOrAdd(assemblyName, name =>
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name)
                    ?? throw new InvalidOperationException($"Assembly '{name}' not found");
                
                var context = new AssemblyHookContext(TestSessionContext.Current!)
                {
                    Assembly = assembly
                };
                
                AssemblyHookContext.Current = context;
                return context;
            });
            
            // Group by class within assembly
            var testsByClass = assemblyGroup.GroupBy(t => t.Metadata.TestClassType);
            
            foreach (var classGroup in testsByClass)
            {
                var testClassType = classGroup.Key;
                
                // Get or create class context
                var classContext = _classContexts.GetOrAdd(testClassType, type =>
                {
                    var context = new ClassHookContext(assemblyContext)
                    {
                        ClassType = type
                    };
                    
                    ClassHookContext.Current = context;
                    return context;
                });
                
                // Add all tests to the class context
                foreach (var test in classGroup)
                {
                    if (test.Context != null)
                    {
                        classContext.AddTest(test.Context);
                    }
                }
            }
        }
        
        return Task.CompletedTask;
    }
    
    public async Task ExecuteBeforeTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestSessionHooksAsync();
        EnsureContextHierarchy();
        var context = TestSessionContext.Current!;
        
        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeTestSession hook failed: {ex.Message}");
                throw; // Before hooks should prevent execution on failure
            }
        }
    }
    
    public async Task ExecuteAfterTestSessionHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterTestSessionHooksAsync();
        EnsureContextHierarchy();
        var context = TestSessionContext.Current!;
        
        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterTestSession hook failed: {ex.Message}");
                // After hooks failures are logged but don't stop execution
            }
        }
    }
    
    public async Task ExecuteBeforeTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeTestDiscoveryHooksAsync();
        var context = new BeforeTestDiscoveryContext()
        {
            TestFilter = null // Will be set by the discovery process
        };
        
        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeTestDiscovery hook failed: {ex.Message}");
                throw;
            }
        }
    }
    
    public async Task ExecuteAfterTestDiscoveryHooksAsync(CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterTestDiscoveryHooksAsync();
        // Need a parent context - we'll need to pass this in
        var beforeContext = BeforeTestDiscoveryContext.Current ?? new BeforeTestDiscoveryContext { TestFilter = null };
        var context = new TestDiscoveryContext(beforeContext)
        {
            TestFilter = null
        };
        
        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterTestDiscovery hook failed: {ex.Message}");
            }
        }
    }
    
    public async Task OnTestStartingAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        var testClassType = test.Metadata.TestClassType;
        var assemblyName = testClassType.Assembly.GetName().Name ?? "Unknown";
        
        // Track test counts
        _assemblyTestCounts.AddOrUpdate(assemblyName, 1, (_, count) => count + 1);
        _classTestCounts.AddOrUpdate(testClassType, 1, (_, count) => count + 1);
        
        // Execute BeforeAssembly hooks if first test in assembly
        if (_initializedAssemblies.TryAdd(assemblyName, true))
        {
            await ExecuteBeforeAssemblyHooksAsync(assemblyName, cancellationToken);
        }
        
        // Execute BeforeClass hooks if first test in class
        if (_initializedClasses.TryAdd(testClassType, true))
        {
            await ExecuteBeforeClassHooksAsync(testClassType, cancellationToken);
        }
        
        // Add test to class context if it exists and hasn't been added already
        if (_classContexts.TryGetValue(testClassType, out var classContext) && test.Context != null)
        {
            // Check if test is already in the context (from InitializeContextsWithTestsAsync)
            if (!classContext.Tests.Contains(test.Context))
            {
                classContext.AddTest(test.Context);
            }
        }
        
        // Execute BeforeEveryTest hooks
        if (test.Context != null)
        {
            await ExecuteBeforeEveryTestHooksAsync(testClassType, test.Context, cancellationToken);
        }
    }
    
    public async Task OnTestCompletedAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        var testClassType = test.Metadata.TestClassType;
        var assemblyName = testClassType.Assembly.GetName().Name ?? "Unknown";
        
        // Execute AfterEveryTest hooks
        if (test.Context != null)
        {
            await ExecuteAfterEveryTestHooksAsync(testClassType, test.Context, cancellationToken);
        }
        
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
            await ExecuteAfterAssemblyHooksAsync(assemblyName, cancellationToken);
            _assemblyTestCounts.TryRemove(assemblyName, out _);
        }
    }
    
    private async Task ExecuteBeforeAssemblyHooksAsync(string assemblyName, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeAssemblyHooksAsync(assemblyName);
        
        // Get or create assembly context
        var context = _assemblyContexts.GetOrAdd(assemblyName, name =>
        {
            EnsureContextHierarchy();
            
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name)
                ?? throw new InvalidOperationException($"Assembly '{name}' not found");
            
            var assemblyContext = new AssemblyHookContext(TestSessionContext.Current!)
            {
                Assembly = assembly
            };
            
            // Set as current for nested operations
            AssemblyHookContext.Current = assemblyContext;
            return assemblyContext;
        });
        
        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeAssembly hook failed for {assemblyName}: {ex.Message}");
                throw;
            }
        }
    }
    
    private async Task ExecuteAfterAssemblyHooksAsync(string assemblyName, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterAssemblyHooksAsync(assemblyName);
        
        // Use existing assembly context
        if (!_assemblyContexts.TryGetValue(assemblyName, out var context))
        {
            // This shouldn't happen, but create one if needed
            context = _assemblyContexts.GetOrAdd(assemblyName, name =>
            {
                EnsureContextHierarchy();
                
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name)
                    ?? throw new InvalidOperationException($"Assembly '{name}' not found");
                
                var assemblyContext = new AssemblyHookContext(TestSessionContext.Current!)
                {
                    Assembly = assembly
                };
                
                AssemblyHookContext.Current = assemblyContext;
                return assemblyContext;
            });
        }
        
        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterAssembly hook failed for {assemblyName}: {ex.Message}");
            }
        }
    }
    
    private async Task ExecuteBeforeClassHooksAsync(Type testClassType, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeClassHooksAsync(testClassType);
        
        // Get or create class context
        var context = _classContexts.GetOrAdd(testClassType, type =>
        {
            // Ensure assembly context exists
            var assemblyName = type.Assembly.GetName().Name ?? "Unknown";
            AssemblyHookContext? assemblyContext = null;
            
            if (_assemblyContexts.TryGetValue(assemblyName, out assemblyContext))
            {
                // Use existing context
            }
            else if (AssemblyHookContext.Current != null && AssemblyHookContext.Current.Assembly.GetName().Name == assemblyName)
            {
                // Use current context
                assemblyContext = AssemblyHookContext.Current;
            }
            else
            {
                // Create new assembly context
                EnsureContextHierarchy();
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName)
                    ?? throw new InvalidOperationException($"Assembly '{assemblyName}' not found");
                
                assemblyContext = new AssemblyHookContext(TestSessionContext.Current!)
                {
                    Assembly = assembly
                };
                
                AssemblyHookContext.Current = assemblyContext;
                _assemblyContexts[assemblyName] = assemblyContext;
            }
            
            var classContext = new ClassHookContext(assemblyContext)
            {
                ClassType = type
            };
            
            // Set as current for nested operations
            ClassHookContext.Current = classContext;
            return classContext;
        });
        
        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"BeforeClass hook failed for {testClassType.Name}: {ex.Message}");
                throw;
            }
        }
    }
    
    private async Task ExecuteAfterClassHooksAsync(Type testClassType, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectAfterClassHooksAsync(testClassType);
        
        // Use existing class context
        if (!_classContexts.TryGetValue(testClassType, out var context))
        {
            // This shouldn't happen, but create one if needed
            context = _classContexts.GetOrAdd(testClassType, type =>
            {
                // Ensure assembly context exists
                var assemblyName = type.Assembly.GetName().Name ?? "Unknown";
                AssemblyHookContext? assemblyContext = null;
                
                if (_assemblyContexts.TryGetValue(assemblyName, out assemblyContext))
                {
                    // Use existing context
                }
                else if (AssemblyHookContext.Current != null && AssemblyHookContext.Current.Assembly.GetName().Name == assemblyName)
                {
                    // Use current context
                    assemblyContext = AssemblyHookContext.Current;
                }
                else
                {
                    // Create new assembly context
                    EnsureContextHierarchy();
                    var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName)
                        ?? throw new InvalidOperationException($"Assembly '{assemblyName}' not found");
                    
                    assemblyContext = new AssemblyHookContext(TestSessionContext.Current!)
                    {
                        Assembly = assembly
                    };
                    
                    AssemblyHookContext.Current = assemblyContext;
                    _assemblyContexts[assemblyName] = assemblyContext;
                }
                
                var classContext = new ClassHookContext(assemblyContext)
                {
                    ClassType = type
                };
                
                ClassHookContext.Current = classContext;
                return classContext;
            });
        }
        
        foreach (var hook in hooks)
        {
            try
            {
                await hook(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterClass hook failed for {testClassType.Name}: {ex.Message}");
            }
        }
    }
    
    private async Task ExecuteBeforeEveryTestHooksAsync(Type testClassType, TestContext testContext, CancellationToken cancellationToken)
    {
        var hooks = await _hookCollectionService.CollectBeforeEveryTestHooksAsync(testClassType);
        
        foreach (var hook in hooks)
        {
            try
            {
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
                await hook(testContext, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"AfterEveryTest hook failed: {ex.Message}");
            }
        }
    }
}