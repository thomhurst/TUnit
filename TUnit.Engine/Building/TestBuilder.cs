using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
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

    public async Task<ExecutableTest> BuildTestAsync(ExpandedTestData expandedData)
    {
        var metadata = expandedData.Metadata;

        // Generate unique test ID
        var testId = GenerateTestId(metadata, expandedData.DataSourceIndices);

        // Generate display name
        var displayName = GenerateDisplayName(metadata, expandedData.ArgumentsDisplayText);

        // Create instance factory
        var createInstance = CreateInstanceFactory(metadata, expandedData);

        // Create test context first since it's required
        var context = await CreateTestContextAsync(testId, displayName, metadata, createInstance);

        // Create hooks using the HookCollectionService
        var beforeTestHooks = await CreateTestHooksAsync(metadata.TestClassType, isBeforeHook: true);
        var afterTestHooks = await CreateTestHooksAsync(metadata.TestClassType, isBeforeHook: false);

        // Build the executable test with all required properties
        var executableTest = new DynamicExecutableTest(createInstance, metadata.TestInvoker!)
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = expandedData.MethodArgumentsFactory(),
            ClassArguments = expandedData.ClassArgumentsFactory(),
            PropertyValues = new Dictionary<string, object?>(), // Will be populated by factory
            BeforeTestHooks = beforeTestHooks,
            AfterTestHooks = afterTestHooks,
            Context = context
        };

        return executableTest;
    }

    private Func<Task<object>> CreateInstanceFactory(TestMetadata metadata, ExpandedTestData expandedData)
    {
        if (metadata.InstanceFactory == null)
        {
            throw new InvalidOperationException(
                $"No instance factory provided for test class {metadata.TestClassType}. " +
                "Ensure tests are either source-generated or discovered via reflection with proper factory initialization.");
        }

        return async () =>
        {
            var classArgs = expandedData.ClassArgumentsFactory();
            var instance = metadata.InstanceFactory(classArgs);
            await InjectPropertiesAsync(instance, expandedData.PropertyFactories);
            return instance;
        };
    }


    private async Task InjectPropertiesAsync(object instance, Dictionary<string, Func<object?>> propertyFactories)
    {
        // Property injection is handled by source-generated code
        // The propertyFactories are pre-compiled setters that will be invoked
        // This is a placeholder for future enhancement where we might need
        // to coordinate with generated property injection code
        await Task.CompletedTask;
    }

    private async Task<Func<TestContext, CancellationToken, Task>[]> CreateTestHooksAsync(Type testClassType, bool isBeforeHook)
    {
        var hookCollectionService = _serviceProvider?.GetService(typeof(IHookCollectionService)) as IHookCollectionService;
        if (hookCollectionService == null)
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
        // Create test details
        var testDetails = new TestDetails
        {
            TestId = testId,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = null, // Will be set during execution
            TestMethodArguments = [], // Will be populated by factory
            TestClassArguments = [], // Will be populated by factory
            DisplayName = displayName,
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = typeof(Task), // All test methods return Task in AOT mode
            ClassMetadata = CreateClassMetadata(metadata),
            MethodMetadata = CreateMethodMetadata(metadata)
        };

        // Add categories
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

    [UnconditionalSuppressMessage("AOT", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ClassMetadata.Type.init'", Justification = "Type annotations are handled by source generators")]
    private static ClassMetadata CreateClassMetadata(TestMetadata metadata)
    {
        var type = metadata.TestClassType;

        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () => new ClassMetadata
        {
            Name = type.Name,
            Type = type,
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
            Namespace = type.Namespace,
            Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.FullName ?? "Unknown", () => new AssemblyMetadata
            {
                Name = type.Assembly.GetName().Name ?? "Unknown",
                Attributes = [
                ]
            }),
            Parameters = [],
            Properties = [],
            Parent = null,
            Attributes = []
        });
    }

    [UnconditionalSuppressMessage("AOT", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.MethodMetadata.Type.init'", Justification = "Type annotations are handled by source generators")]
    [UnconditionalSuppressMessage("AOT", "IL2067:'Type' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ParameterMetadata.ParameterMetadata(Type)'", Justification = "Parameter types are known at compile time")]
    private static MethodMetadata CreateMethodMetadata(TestMetadata metadata)
    {
        // In AOT mode, use metadata directly without reflection
        // Create parameters from ParameterTypes array
        var parameters = metadata.ParameterTypes.Select((type, index) => new ParameterMetadata(type)
        {
            Name = $"param{index}",
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
            Attributes = [],
            ReflectionInfo = null! // No reflection info in AOT mode
        }).ToArray();

        // Minimal metadata when MethodInfo is not available
        return new MethodMetadata
        {
            Name = metadata.TestMethodName,
            Type = metadata.TestClassType,
            TypeReference = TypeReference.CreateConcrete(metadata.TestClassType.AssemblyQualifiedName ?? metadata.TestClassType.FullName ?? metadata.TestClassType.Name),
            Class = CreateClassMetadata(metadata),
            Parameters = [],
            GenericTypeCount = 0,
            ReturnTypeReference = TypeReference.CreateConcrete(typeof(Task).AssemblyQualifiedName ?? typeof(Task).FullName ?? "System.Threading.Tasks.Task"),
            ReturnType = typeof(Task),
            Attributes = []
        };
    }
}
