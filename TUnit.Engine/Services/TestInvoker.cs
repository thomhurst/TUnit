using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Exceptions;
using TUnit.Engine.Extensions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;

namespace TUnit.Engine.Services;

internal class TestInvocation(TestHookOrchestrator testHookOrchestrator, Disposer disposer, DataSourceObjectRegistrar dataSourceObjectRegistrar)
{
    private readonly SemaphoreSlim _consoleStandardOutLock = new(1, 1);

    public async Task Invoke(DiscoveredTest discoveredTest, CancellationToken cancellationToken, List<Exception> cleanupExceptions)
    {
        try
        {
            // Register test instance and all data source objects
            var testInstance = discoveredTest.TestContext.TestDetails.ClassInstance;
            dataSourceObjectRegistrar.RegisterExistingDataSourceObjects(testInstance);
            
            // Initialize data sources on the test instance using the simplified API
            var testDetails = discoveredTest.TestContext.TestDetails;
            if (testInstance != null && testDetails.MethodMetadata != null)
            {
                await DataSourceInitializer.InitializeAsync(testInstance, testDetails.MethodMetadata).ConfigureAwait(false);
            }
            
            // Create a list to track all objects that need initialization
            var objectsToInitialize = new List<object>();
            
            // Add test instance
            if (testInstance != null)
            {
                objectsToInitialize.Add(testInstance);
            }
            
            // Add method arguments
            if (testDetails.TestMethodArguments != null)
            {
                objectsToInitialize.AddRange(testDetails.TestMethodArguments.Where(arg => arg != null)!);
            }
            
            // Initialize all objects in proper order (depth-first)
            var visited = new HashSet<object>();
            foreach (var obj in objectsToInitialize)
            {
                await InitializeObjectGraphAsync(obj, visited, cancellationToken).ConfigureAwait(false);
            }
            
            // Now handle the standard initialization objects
            foreach (var onInitializeObject in discoveredTest.TestContext.GetOnInitializeObjects())
            {
                if (!visited.Contains(onInitializeObject))
                {
                    await ObjectInitializer.InitializeAsync(onInitializeObject, cancellationToken);
                }
            }

            await testHookOrchestrator.ExecuteBeforeHooks(discoveredTest, cancellationToken);

            discoveredTest.TestContext.RestoreExecutionContext();

            foreach (var testStartEventsObject in discoveredTest.TestContext.GetTestStartEventObjects())
            {
                await testStartEventsObject.OnTestStart(new BeforeTestContext(discoveredTest));
            }

            await Timings.Record("Test Body", discoveredTest.TestContext,
                () => discoveredTest.ExecuteTest(cancellationToken));

            discoveredTest.TestContext.SetResult(null);
        }
        catch (Exception ex)
        {
            discoveredTest.TestContext.SetResult(ex);
            throw;
        }
        finally
        {
            await DisposeTest(discoveredTest.TestContext, cleanupExceptions);
        }
    }

    private async ValueTask DisposeTest(TestContext testContext, List<Exception> cleanUpExceptions)
    {
        var afterHooks = testHookOrchestrator.CollectAfterHooks(testContext.TestDetails.ClassInstance, testContext.InternalDiscoveredTest, cleanUpExceptions);

        foreach (var executableHook in afterHooks)
        {
            await Timings.Record($"After(Test): {executableHook.Name}", testContext, () =>
            {
                try
                {
                    return executableHook.ExecuteAsync(testContext, CancellationToken.None);
                }
                catch (Exception e)
                {
                    throw new HookFailedException($"Error executing [After(Test)] hook: {executableHook.MethodInfo.Type.FullName}.{executableHook.Name}", e);
                }
            });
        }

        foreach (var testEndEventsObject in testContext.GetTestEndEventObjects())
        {
            await RunHelpers.RunValueTaskSafelyAsync(() => testEndEventsObject.OnTestEnd(new AfterTestContext(testContext.InternalDiscoveredTest)),
                cleanUpExceptions);
        }

        foreach (var disposableObject in testContext.GetOnDisposeObjects())
        {
            await RunHelpers.RunValueTaskSafelyAsync(() => disposer.DisposeAsync(disposableObject),
                cleanUpExceptions);
        }

        await _consoleStandardOutLock.WaitAsync();

        try
        {
            await disposer.DisposeAsync(testContext);
        }
        finally
        {
            _consoleStandardOutLock.Release();
        }
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
    private async Task InitializeObjectGraphAsync(object obj, HashSet<object> visited, CancellationToken cancellationToken)
    {
        if (!visited.Add(obj))
        {
            return; // Already processed
        }

        var objType = obj.GetType();
        
        // Skip primitive types and strings
        if (objType.IsPrimitive || obj is string || objType.IsEnum)
        {
            return;
        }

        // First, recursively initialize all property values (depth-first)
        var properties = objType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                if (value != null && !property.PropertyType.IsPrimitive && !(value is string) && !property.PropertyType.IsEnum)
                {
                    await InitializeObjectGraphAsync(value, visited, cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                // Skip properties that throw when accessed
                continue;
            }
        }

        // Then initialize this object if it implements IAsyncInitializer
        // This ensures children are initialized before parents
        await ObjectInitializer.InitializeAsync(obj, cancellationToken).ConfigureAwait(false);
    }
}
