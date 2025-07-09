using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Helpers;
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
    public async Task<IEnumerable<ExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata)
    {
        var tests = new List<ExecutableTest>();

        try
        {
            // Use the DataCombinationGenerator directly - no reflection needed
            var asyncCombinations = metadata.DataCombinationGenerator();
            await foreach (var combination in asyncCombinations)
            {
                // Check if this combination requires runtime data generation
                if (combination.IsRuntimeGenerated)
                {
                    // Handle runtime data source generators
                    await foreach (var runtimeCombination in GenerateRuntimeCombinations(metadata))
                    {
                        var test = await BuildTestAsync(metadata, runtimeCombination);
                        tests.Add(test);
                    }
                }
                else
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
        // Generate unique test ID
        var testId = GenerateTestId(metadata, combination);

        var displayName = GenerateDisplayName(metadata, GetArgumentsDisplayText(combination));

        var context = await CreateTestContextAsync(testId, displayName, metadata);

        await InvokeDiscoveryEventReceiversAsync(metadata, context);

        var beforeTestHooks = await CreateTestHooksAsync(metadata.TestClassType, isBeforeHook: true);
        var afterTestHooks = await CreateTestHooksAsync(metadata.TestClassType, isBeforeHook: false);

        // Use the CreateExecutableTestFactory directly - no reflection needed
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

        return metadata.CreateExecutableTestFactory(creationContext, metadata);
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

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Type information is preserved by source generation")]
    private async IAsyncEnumerable<TestDataCombination> GenerateRuntimeCombinations(TestMetadata metadata)
    {
        await foreach (var combination in RuntimeDataSourceHelper.GenerateDataCombinationsAsync(
            metadata.TestClassType,
            metadata.TestMethodName,
            metadata.AttributeFactory))
        {
            yield return combination;
        }
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

    private static string GenerateTestId(TestMetadata metadata, TestDataCombination combination)
    {
        var methodMetadata = metadata.MethodMetadata;

        var classMetadata = methodMetadata.Class;

        var constructorParameters = classMetadata.Parameters.Select(x => x.Type);

        var methodParameters = methodMetadata.Parameters.Select(x => x.Type);

        return $"{methodMetadata.Class.Namespace}.{metadata.TestClassType.Name}({string.Join(", ", constructorParameters)}).{combination.ClassDataSourceIndex}.{combination.ClassLoopIndex}.{metadata.TestMethodName}({string.Join(", ", methodParameters)}).{combination.MethodDataSourceIndex}.{combination.MethodLoopIndex}";
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
        var testId = metadata.TestId ?? $"{metadata.TestClassType.FullName}.{metadata.TestMethodName}_DataGenerationError";
        var displayName = customDisplayName ?? $"{metadata.TestName} [DATA GENERATION ERROR]";

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
            MethodMetadata = MetadataBuilder.CreateMethodMetadata(metadata),
            Attributes = [],
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
