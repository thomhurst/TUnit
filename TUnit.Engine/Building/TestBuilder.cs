using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Interfaces;

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
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection is only used as a fallback for non-AOT scenarios")]
    public async Task<IEnumerable<ExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata)
    {
        var tests = new List<ExecutableTest>();

        // Check if this is a typed metadata with DataCombinationGenerator
        if (metadata is TestMetadata<object> typedMetadata)
        {
            var dataGeneratorProp = metadata.GetType().GetProperty("DataCombinationGenerator");
            if (dataGeneratorProp?.GetValue(metadata) is Delegate dataCombinationGenerator)
            {
                try
                {
                    // Invoke the DataCombinationGenerator delegate
                    var invokeMethod = dataCombinationGenerator.GetType().GetMethod("Invoke");
                    if (invokeMethod?.Invoke(dataCombinationGenerator, null) is IAsyncEnumerable<TestDataCombination> asyncCombinations)
                    {
                        await foreach (var combination in asyncCombinations)
                        {
                            var test = await BuildTestAsync(metadata, combination);
                            tests.Add(test);
                        }
                        return tests;
                    }
                }
                catch (Exception ex)
                {
                    // If data combination generation fails, create a failed test
                    var failedTest = CreateFailedTestForDataGenerationError(metadata, ex);
                    tests.Add(failedTest);
                    return tests;
                }
            }
        }

        // Fallback: Create a single test without data combinations (no arguments)
        var defaultCombination = new TestDataCombination();
        var defaultTest = await BuildTestAsync(metadata, defaultCombination);
        tests.Add(defaultTest);
        return tests;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection is only used as a fallback for non-AOT scenarios")]
    public async Task<ExecutableTest> BuildTestAsync(TestMetadata metadata, TestDataCombination combination)
    {
        // Generate unique test ID
        var testId = GenerateTestId(metadata, combination.DataSourceIndices);

        var displayName = GenerateDisplayName(metadata, GetArgumentsDisplayText(combination));

        var createInstance = CreateInstanceFactory(metadata, combination);

        var context = await CreateTestContextAsync(testId, displayName, metadata, createInstance);

        await InvokeDiscoveryEventReceiversAsync(metadata, context);

        var beforeTestHooks = await CreateTestHooksAsync(metadata.TestClassType, isBeforeHook: true);
        var afterTestHooks = await CreateTestHooksAsync(metadata.TestClassType, isBeforeHook: false);

        // Check if we have a typed metadata with CreateExecutableTest factory
        if (metadata is TestMetadata<object> baseTypedMetadata)
        {
            // Get the actual CreateExecutableTest delegate via the interface
            var createExecutableTestProp = metadata.GetType().GetProperty("CreateExecutableTest");
            if (createExecutableTestProp != null)
            {
                var createExecutableTestValue = createExecutableTestProp.GetValue(metadata);
                if (createExecutableTestValue != null)
                {
                    var creationContext = new ExecutableTestCreationContext
                    {
                        TestId = testId,
                        DisplayName = displayName,
                        Arguments = combination.MethodData,
                        ClassArguments = combination.ClassData,
                        PropertyValues = combination.PropertyValues,
                        BeforeTestHooks = beforeTestHooks,
                        AfterTestHooks = afterTestHooks,
                        Context = context
                    };
                    
                    // Try to invoke it dynamically
                    try
                    {
                        var methodInfo = createExecutableTestValue.GetType().GetMethod("Invoke");
                        if (methodInfo != null)
                        {
                            var executableTest = methodInfo.Invoke(createExecutableTestValue, new object[] { creationContext, metadata }) as ExecutableTest;
                            if (executableTest != null)
                            {
                                return executableTest;
                            }
                        }
                    }
                    catch
                    {
                        // Fall back to DynamicExecutableTest
                    }
                }
            }
        }
        
        // Fallback to DynamicExecutableTest for non-typed metadata
        
        var dynamicExecutableTest = new DynamicExecutableTest(createInstance, metadata.TestInvoker!)
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = combination.MethodData,
            ClassArguments = combination.ClassData,
            PropertyValues = combination.PropertyValues,
            BeforeTestHooks = beforeTestHooks,
            AfterTestHooks = afterTestHooks,
            Context = context
        };

        return dynamicExecutableTest;
    }

    private static string GetArgumentsDisplayText(TestDataCombination combination)
    {
        var allArgs = new List<object?>();
        allArgs.AddRange(combination.ClassData);
        allArgs.AddRange(combination.MethodData);
        
        if (allArgs.Count == 0)
        {
            return string.Empty;
        }
        
        return string.Join(", ", allArgs.Select(arg => arg?.ToString() ?? "null"));
    }


    private Func<Task<object>> CreateInstanceFactory(TestMetadata metadata, TestDataCombination combination)
    {
        if (metadata.InstanceFactory == null)
        {
            throw new InvalidOperationException(
                $"No instance factory provided for test class {metadata.TestClassType}. " +
                "Ensure tests are either source-generated or discovered via reflection with proper factory initialization.");
        }

        return () =>
        {
            object instance;
            if (combination.PropertyValues.Any())
            {
                var argsWithProperties = combination.ClassData.Concat(new object[] { combination.PropertyValues }).ToArray();
                instance = metadata.InstanceFactory(argsWithProperties);
            }
            else
            {
                instance = metadata.InstanceFactory(combination.ClassData);
            }
            
            return Task.FromResult(instance);
        };
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

    private static string GenerateTestId(TestMetadata metadata, int[] dataSourceIndices)
    {
        var parts = new List<string> { metadata.TestId };

        if (dataSourceIndices.Length > 0)
        {
            var dsIndexPart = string.Join(".", dataSourceIndices);
            parts.Add($"ds{dsIndexPart}");
        }

        return string.Join("_", parts);
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

    private async Task<TestContext> CreateTestContextAsync(string testId, string displayName, TestMetadata metadata, 
        Func<Task<object>> createInstance)
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
            MethodMetadata = MetadataBuilder.CreateMethodMetadata(metadata)
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
        var attributes = metadata.AttributeFactory?.Invoke() ?? Array.Empty<Attribute>();
        
        context.TestDetails.Attributes = attributes;

        var discoveredContext = new DiscoveredTestContext(
            context.TestDetails.TestName,
            context.TestDetails.DisplayName ?? context.TestDetails.TestName,
            context.TestDetails);

        foreach (var attribute in attributes)
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

        discoveredContext.TransferTo(context);
        
        if (context.TestDetails.DisplayName != discoveredContext.DisplayName)
        {
            context.TestDetails.DisplayName = discoveredContext.DisplayName;
        }
    }

    private static ExecutableTest CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
    {
        var testId = metadata.TestId ?? $"{metadata.TestClassType.FullName}.{metadata.TestMethodName}_DataGenerationError";
        var displayName = $"{metadata.TestName} [DATA GENERATION ERROR]";

        // Create a minimal test context for failed test
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
            MethodMetadata = MetadataBuilder.CreateMethodMetadata(metadata)
        };

        var context = new TestContext(
            metadata.TestName,
            displayName,
            CancellationToken.None,
            new TUnit.Core.Services.TestServiceProvider())
        {
            TestDetails = testDetails
        };

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
}
