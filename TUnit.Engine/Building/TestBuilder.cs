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

        var executableTest = new DynamicExecutableTest(createInstance, metadata.TestInvoker!)
        {
            TestId = testId,
            DisplayName = displayName,
            Metadata = metadata,
            Arguments = expandedData.MethodArgumentsFactory(),
            ClassArguments = expandedData.ClassArgumentsFactory(),
            PropertyValues = new Dictionary<string, object?>(),
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
            ClassMetadata = CreateClassMetadata(metadata),
            MethodMetadata = CreateMethodMetadata(metadata)
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
        metadata.ParameterTypes.Select((type, index) => new ParameterMetadata(type)
        {
            Name = $"param{index}",
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
            Attributes = [],
            ReflectionInfo = null!
        }).ToArray();

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
