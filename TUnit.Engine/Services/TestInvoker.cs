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
            
            foreach (var onInitializeObject in discoveredTest.TestContext.GetOnInitializeObjects())
            {
                // For objects with [ClassDataSource] properties, ensure they are populated first
                if (onInitializeObject == testInstance)
                {
                    // The test instance needs its data source properties populated before initialization
                    var testDetails = discoveredTest.TestContext.TestDetails;
                    var methodInfo = testDetails.TestMethod;
                    
                    // Create metadata for property initialization
                    var dataGeneratorMetadata = new DataGeneratorMetadata
                    {
                        Type = DataGeneratorType.Property,
                        TestInformation = methodInfo,
                        ClassInstanceArguments = testDetails.TestClassArguments ?? [],
                        MembersToGenerate = [],
                        TestBuilderContext = null!,
                        TestClassInstance = testInstance,
                        TestSessionId = TestSessionContext.Current?.Id ?? string.Empty
                    };
                    
                    // Initialize properties with data sources before calling InitializeAsync
                    await DataSourceInitializer.InitializeAsync(
                        testInstance,
                        dataGeneratorMetadata,
                        null,
                        obj => dataSourceObjectRegistrar.RegisterExistingDataSourceObjects(obj)
                    ).ConfigureAwait(false);
                }
                else
                {
                    // For other objects, just initialize them normally
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
}
