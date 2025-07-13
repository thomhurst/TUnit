using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.Engine.Building;

/// <summary>
/// Builds executable tests from expanded test data
/// </summary>
public sealed class TestBuilder : ITestBuilder
{
    private readonly IServiceProvider? _serviceProvider;

    public TestBuilder(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
    }


    /// <summary>
    /// Builds all executable tests from a single TestMetadata using its DataCombinationGenerator delegate.
    /// This is the new simplified approach that replaces DataSourceExpander.
    /// </summary>
    public async Task<IEnumerable<ExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata)
    {
        var tests = new List<ExecutableTest>();

        try
        {
            // Use the DataCombinationGenerator directly - no reflection needed
            var asyncCombinations = metadata.DataCombinationGenerator();
            await foreach (var combination in asyncCombinations)
            {
                // Check if this combination has a data generation exception
                if (combination.DataGenerationException != null)
                {
                    var failedTest = CreateFailedTestForDataGenerationError(metadata, combination.DataGenerationException, combination.DisplayName);
                    tests.Add(failedTest);
                }
                else
                {
                    var test = await BuildTestAsync(metadata, combination);
                    tests.Add(test);
                }
            }
        }
        catch (Exception ex)
        {
            // If data combination generation fails, create a failed test
            var failedTest = CreateFailedTestForDataGenerationError(metadata, ex);
            tests.Add(failedTest);
            return tests;
        }

        return tests;
    }

    public async Task<ExecutableTest> BuildTestAsync(TestMetadata metadata, TestDataCombination combination)
    {
        // Create fresh instances from factories
        var classArguments = await CreateArgumentsFromFactoriesAsync(combination.ClassDataFactories);
        var methodArguments = await CreateArgumentsFromFactoriesAsync(combination.MethodDataFactories);
        var propertyValues = new Dictionary<string, object?>();
        foreach (var kvp in combination.PropertyValueFactories)
        {
            propertyValues[kvp.Key] = await kvp.Value();
        }
        
        // Track all objects from data sources
        TrackDataSourceObjects(classArguments, methodArguments, propertyValues);
        
        // Generate unique test ID
        var testId = TestIdentifierService.GenerateTestId(metadata, combination);

        var displayName = GenerateDisplayName(metadata, await GetArgumentsDisplayTextAsync(combination));

        var context = await CreateTestContextAsync(testId, displayName, metadata);
        
        // Transfer property values to TestDetails
        foreach (var kvp in propertyValues)
        {
            context.TestDetails.TestClassInjectedPropertyArguments[kvp.Key] = kvp.Value;
        }

        await InvokeDiscoveryEventReceiversAsync(metadata, context);

        var beforeTestHooks = await CreateTestHooksAsync(metadata.TestClassType, isBeforeHook: true);
        var afterTestHooks = await CreateTestHooksAsync(metadata.TestClassType, isBeforeHook: false);

        // Use the CreateExecutableTestFactory directly - no reflection needed
        var creationContext = new ExecutableTestCreationContext
        {
            TestId = testId,
            DisplayName = displayName,
            Arguments = methodArguments,
            ClassArguments = classArguments,
            PropertyValues = propertyValues,
            BeforeTestHooks = beforeTestHooks,
            AfterTestHooks = afterTestHooks,
            Context = context
        };

        return metadata.CreateExecutableTestFactory(creationContext, metadata);
    }

    private static async Task<string> GetArgumentsDisplayTextAsync(TestDataCombination combination)
    {
        var allArgs = new List<object?>();
        allArgs.AddRange(await Task.WhenAll(combination.ClassDataFactories.Select(f => f())));
        allArgs.AddRange(await Task.WhenAll(combination.MethodDataFactories.Select(f => f())));

        if (allArgs.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(", ", allArgs.Select(arg => arg?.ToString() ?? "null"));
    }

    private async Task<Func<TestContext, CancellationToken, Task>[]> CreateTestHooksAsync(Type testClassType, bool isBeforeHook)
    {
        if (_serviceProvider?.GetService(typeof(IHookCollectionService)) is not IHookCollectionService hookCollectionService)
        {
            return Array.Empty<Func<TestContext, CancellationToken, Task>>();
        }

        var hooks = isBeforeHook
            ? await hookCollectionService.CollectBeforeTestHooksAsync(testClassType)
            : await hookCollectionService.CollectAfterTestHooksAsync(testClassType);

        return hooks.ToArray();
    }


    private static string GenerateDisplayName(TestMetadata metadata, string argumentsDisplayText)
    {
        var displayName = metadata.TestName;

        if (!string.IsNullOrEmpty(argumentsDisplayText))
        {
            displayName += $"({argumentsDisplayText})";
        }

        return displayName;
    }

    private async Task<TestContext> CreateTestContextAsync(string testId, string displayName, TestMetadata metadata)
    {
        var testDetails = new TestDetails
        {
            TestId = testId,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = null,
            TestMethodArguments = [],
            TestClassArguments = [],
            DisplayName = displayName,
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = typeof(Task),
            ClassMetadata = MetadataBuilder.CreateClassMetadata(metadata),
            MethodMetadata = MetadataBuilder.CreateMethodMetadata(metadata),
            Attributes =  metadata.AttributeFactory.Invoke()
        };

        foreach (var category in metadata.Categories)
        {
            testDetails.Categories.Add(category);
        }

        var context = new TestContext(
            metadata.TestName,
            displayName,
            CancellationToken.None,
            _serviceProvider ?? new TUnit.Core.Services.TestServiceProvider())
        {
            TestDetails = testDetails
        };

        return await Task.FromResult(context);
    }


    private async Task InvokeDiscoveryEventReceiversAsync(TestMetadata metadata, TestContext context)
    {
        var discoveredContext = new DiscoveredTestContext(
            context.TestDetails.TestName,
            context.TestDetails.DisplayName ?? context.TestDetails.TestName,
            context.TestDetails);

        // Try to get EventReceiverOrchestrator from service provider
        var eventReceiverOrchestrator = _serviceProvider?.GetService(typeof(OptimizedEventReceiverOrchestrator)) as OptimizedEventReceiverOrchestrator;
        if (eventReceiverOrchestrator != null)
        {
            // Use the orchestrator for consistency with other event receivers
            await eventReceiverOrchestrator.InvokeTestDiscoveryEventReceiversAsync(context, discoveredContext, CancellationToken.None);
        }
        else
        {
            // Fallback to attribute-only if orchestrator not available
            foreach (var attribute in context.TestDetails.Attributes)
            {
                if (attribute is ITestDiscoveryEventReceiver receiver)
                {
                    try
                    {
                        await receiver.OnTestDiscovered(discoveredContext);
                    }
                    catch (Exception ex)
                    {
                        _ = ex;
                    }
                }
            }
        }

        discoveredContext.TransferTo(context);

        if (context.TestDetails.DisplayName != discoveredContext.DisplayName)
        {
            context.TestDetails.DisplayName = discoveredContext.DisplayName;
        }
    }

    private static ExecutableTest CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
    {
        return CreateFailedTestForDataGenerationError(metadata, exception, null);
    }

    private static ExecutableTest CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception, string? customDisplayName)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata);
        var displayName = customDisplayName ?? $"{metadata.TestName} [DATA GENERATION ERROR]";

        var testDetails = CreateFailedTestDetails(metadata, testId, displayName);
        var context = CreateFailedTestContext(metadata, testDetails, displayName);

        return new FailedExecutableTest(exception)
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = [],
            ClassArguments = [],
            BeforeTestHooks = [],
            AfterTestHooks = [],
            Context = context
        };
    }

    private static TestDetails CreateFailedTestDetails(TestMetadata metadata, string testId, string displayName)
    {
        return new TestDetails
        {
            TestId = testId,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = null,
            TestMethodArguments = [],
            TestClassArguments = [],
            DisplayName = displayName,
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = typeof(Task),
            ClassMetadata = MetadataBuilder.CreateClassMetadata(metadata),
            MethodMetadata = MetadataBuilder.CreateMethodMetadata(metadata),
            Attributes = [],
        };
    }

    private static TestContext CreateFailedTestContext(TestMetadata metadata, TestDetails testDetails, string displayName)
    {
        return new TestContext(
            metadata.TestName,
            displayName,
            CancellationToken.None,
            new TUnit.Core.Services.TestServiceProvider())
        {
            TestDetails = testDetails
        };
    }
    
    private static void TrackDataSourceObjects(object?[] classArguments, object?[] methodArguments, Dictionary<string, object?> propertyValues)
    {
        ActiveObjectTracker.IncrementUsage(classArguments);
        ActiveObjectTracker.IncrementUsage(methodArguments);
        ActiveObjectTracker.IncrementUsage(propertyValues.Values);
    }

    /// <summary>
    /// Efficiently create arguments array from factories without LINQ overhead
    /// </summary>
    private static async Task<object?[]> CreateArgumentsFromFactoriesAsync(IReadOnlyList<Func<Task<object?>>> factories)
    {
        if (factories.Count == 0)
            return [];

        var arguments = new object?[factories.Count];
        var tasks = new Task<object?>[factories.Count];
        
        // Start all tasks
        for (int i = 0; i < factories.Count; i++)
        {
            tasks[i] = factories[i]();
        }
        
        // Wait for all and collect results - safe to use Result after WhenAll
        var results = await Task.WhenAll(tasks);
        for (int i = 0; i < results.Length; i++)
        {
            arguments[i] = results[i];
        }
        
        return arguments;
    }
}
