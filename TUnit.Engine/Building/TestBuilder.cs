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

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection is only used as a fallback for non-AOT scenarios")]
    public async Task<ExecutableTest> BuildTestAsync(ExpandedTestData expandedData)
    {
        var metadata = expandedData.Metadata;

        // Generate unique test ID
        var testId = GenerateTestId(metadata, expandedData.DataSourceIndices);

        var displayName = GenerateDisplayName(metadata, expandedData.ArgumentsDisplayText);

        var createInstance = CreateInstanceFactory(metadata, expandedData);

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
                    var typedMethodArgs = expandedData.MethodArgumentsFactory() ?? Array.Empty<object?>();
                    var typedClassArgs = expandedData.ClassArgumentsFactory() ?? Array.Empty<object?>();
                    
                    var creationContext = new ExecutableTestCreationContext
                    {
                        TestId = testId,
                        DisplayName = displayName,
                        Arguments = typedMethodArgs!,
                        ClassArguments = typedClassArgs!,
                        PropertyValues = new Dictionary<string, object?>(),
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
        var methodArgs = expandedData.MethodArgumentsFactory() ?? Array.Empty<object?>();
        var classArgs = expandedData.ClassArgumentsFactory() ?? Array.Empty<object?>();
        
        var dynamicExecutableTest = new DynamicExecutableTest(createInstance, metadata.TestInvoker!)
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = methodArgs,
            ClassArguments = classArgs,
            PropertyValues = new Dictionary<string, object?>(),
            BeforeTestHooks = beforeTestHooks,
            AfterTestHooks = afterTestHooks,
            Context = context
        };

        return dynamicExecutableTest;
    }

    private Func<Task<object>> CreateInstanceFactory(TestMetadata metadata, ExpandedTestData expandedData)
    {
        if (metadata.InstanceFactory == null)
        {
            throw new InvalidOperationException(
                $"No instance factory provided for test class {metadata.TestClassType}. " +
                "Ensure tests are either source-generated or discovered via reflection with proper factory initialization.");
        }

        return () =>
        {
            var classArgs = expandedData.ClassArgumentsFactory();
            
            object instance;
            if (expandedData.PropertyFactories.Any())
            {
                var propertyValues = new Dictionary<string, object?>();
                foreach (var kvp in expandedData.PropertyFactories)
                {
                    propertyValues[kvp.Key] = kvp.Value();
                }
                
                var argsWithProperties = classArgs.Concat(new object[] { propertyValues }).ToArray();
                instance = metadata.InstanceFactory(argsWithProperties);
            }
            else
            {
                instance = metadata.InstanceFactory(classArgs);
            }
            
            return Task.FromResult(instance);
        };
    }


    private async Task InjectPropertiesAsync(object instance, TestMetadata metadata, Dictionary<string, Func<object?>> propertyFactories)
    {
        foreach (var kvp in propertyFactories)
        {
            var propertyName = kvp.Key;
            var valueFactory = kvp.Value;
            
            if (metadata.PropertySetters.TryGetValue(propertyName, out var setter))
            {
                var value = valueFactory();
                setter(instance, value);
            }
            else
            {
                var injection = metadata.PropertyInjections.FirstOrDefault(pi => pi.PropertyName == propertyName);
                if (injection != null)
                {
                    var value = valueFactory();
                    injection.Setter(instance, value);
                }
            }
        }
        
        await Task.CompletedTask;
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
}
