using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

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

        // Create test invoker
        var invokeTest = CreateTestInvoker(metadata, expandedData);

        // Create hooks
        var hooks = CreateHooks(metadata);

        // Build the executable test
        var executableTest = new ExecutableTest
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = [], // Will be populated by factory
            ClassArguments = [], // Will be populated by factory
            CreateInstance = createInstance,
            InvokeTest = invokeTest,
            PropertyValues = new Dictionary<string, object?>(), // Will be populated by factory
            Hooks = hooks
        };

        // Create test context
        executableTest.Context = await CreateTestContextAsync(executableTest);

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

    private Func<object, Task> CreateTestInvoker(TestMetadata metadata, ExpandedTestData expandedData)
    {
        if (metadata.TestInvoker == null)
        {
            throw new InvalidOperationException(
                $"No test invoker provided for test {metadata.TestName}. " +
                "Ensure tests are either source-generated or discovered via reflection with proper invoker initialization.");
        }

        return async instance =>
        {
            var methodArgs = expandedData.MethodArgumentsFactory();
            await metadata.TestInvoker(instance, methodArgs);
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

    private TestLifecycleHooks CreateHooks(TestMetadata metadata)
    {
        return new TestLifecycleHooks
        {
            BeforeClass = CreateStaticHookInvokers(metadata.Hooks.BeforeClass),
            AfterClass = CreateInstanceHookInvokers(metadata.Hooks.AfterClass),
            BeforeTest = CreateInstanceHookInvokers(metadata.Hooks.BeforeTest),
            AfterTest = CreateInstanceHookInvokers(metadata.Hooks.AfterTest)
        };
    }

    private Func<HookContext, Task>[] CreateStaticHookInvokers(HookMetadata[] hooks)
    {
        return hooks.Select(h => new Func<HookContext, Task>(async context =>
        {
            if (h.Invoker != null)
            {
                await h.Invoker(null, context);
            }
        })).ToArray();
    }

    private Func<object, HookContext, Task>[] CreateInstanceHookInvokers(HookMetadata[] hooks)
    {
        return hooks.Select(h => new Func<object, HookContext, Task>(async (instance, context) =>
        {
            if (h.Invoker != null)
            {
                await h.Invoker(instance, context);
            }
        })).ToArray();
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

    private async Task<TestContext> CreateTestContextAsync(ExecutableTest test)
    {
        // Create test details
        var testDetails = new TestDetails
        {
            TestId = test.TestId,
            TestName = test.Metadata.TestName,
            ClassType = test.Metadata.TestClassType,
            MethodName = test.Metadata.TestMethodName,
            ClassInstance = null, // Will be set during execution
            TestMethodArguments = test.Arguments,
            TestClassArguments = test.ClassArguments,
            DisplayName = test.DisplayName,
            TestFilePath = test.Metadata.FilePath ?? "Unknown",
            TestLineNumber = test.Metadata.LineNumber ?? 0,
            TestMethodParameterTypes = test.Metadata.ParameterTypes,
            ReturnType = typeof(Task), // All test methods return Task in AOT mode
            ClassMetadata = CreateClassMetadata(test.Metadata),
            MethodMetadata = CreateMethodMetadata(test.Metadata)
        };

        // Add categories
        foreach (var category in test.Metadata.Categories)
        {
            testDetails.Categories.Add(category);
        }

        var context = new TestContext(
            test.Metadata.TestName, 
            test.DisplayName, 
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
