using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building;

/// <summary>
/// Builds executable tests from expanded test data
/// </summary>
public sealed class TestBuilder : ITestBuilder
{
    private readonly ITestInvoker _testInvoker;
    private readonly IHookInvoker _hookInvoker;
    private readonly bool _isAotMode;

    public TestBuilder(ITestInvoker testInvoker, IHookInvoker hookInvoker, bool isAotMode = true)
    {
        _testInvoker = testInvoker ?? throw new ArgumentNullException(nameof(testInvoker));
        _hookInvoker = hookInvoker ?? throw new ArgumentNullException(nameof(hookInvoker));
        _isAotMode = isAotMode;
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
            Arguments = Array.Empty<object?>(), // Will be populated by factory
            ClassArguments = Array.Empty<object?>(), // Will be populated by factory
            CreateInstance = createInstance,
            InvokeTest = invokeTest,
            PropertyValues = new Dictionary<string, object?>(), // Will be populated by factory
            Hooks = hooks
        };

        // Create test context
        executableTest.Context = await CreateTestContextAsync(executableTest);

        return executableTest;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "Calls to reflection methods are guarded by isAotMode check and only occur when AOT factories are not available")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break functionality when trimming application code", Justification = "Calls to reflection methods are fallbacks when trimming-safe pre-compiled factories are not available")]
    private Func<Task<object>> CreateInstanceFactory(TestMetadata metadata, ExpandedTestData expandedData)
    {
        if (_isAotMode && metadata.InstanceFactory != null)
        {
            // AOT mode with pre-compiled factory
            return async () =>
            {
                var classArgs = expandedData.ClassArgumentsFactory();
                var instance = metadata.InstanceFactory(classArgs);
                await InjectPropertiesAsync(instance, expandedData.PropertyFactories);
                return instance;
            };
        }

        // Reflection mode
        return CreateReflectionInstanceFactory(metadata, expandedData);
    }

    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private Func<Task<object>> CreateReflectionInstanceFactory(TestMetadata metadata, ExpandedTestData expandedData)
    {
        return async () =>
        {
            var classType = metadata.TestClassType;
            var classArgs = expandedData.ClassArgumentsFactory();

            var instance = classArgs.Length > 0
                ? Activator.CreateInstance(classType, classArgs)
                : Activator.CreateInstance(classType);

            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to create instance of {classType}");
            }

            await InjectPropertiesAsync(instance, expandedData.PropertyFactories);
            return instance;
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "Calls to reflection methods are guarded by isAotMode check and only occur when AOT invokers are not available")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break functionality when trimming application code", Justification = "Calls to reflection methods are fallbacks when trimming-safe pre-compiled invokers are not available")]
    private Func<object, Task> CreateTestInvoker(TestMetadata metadata, ExpandedTestData expandedData)
    {
        if (_isAotMode && metadata.TestInvoker != null)
        {
            // AOT mode with pre-compiled invoker
            return async instance =>
            {
                var methodArgs = expandedData.MethodArgumentsFactory();
                await metadata.TestInvoker(instance, methodArgs);
            };
        }

        // Reflection mode
        return CreateReflectionTestInvoker(metadata, expandedData);
    }

    [RequiresDynamicCode("Reflection mode requires dynamic code generation")]
    [RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
    private Func<object, Task> CreateReflectionTestInvoker(TestMetadata metadata, ExpandedTestData expandedData)
    {
        if (metadata.MethodInfo == null)
        {
            throw new InvalidOperationException($"No invoker or MethodInfo available for test {metadata.TestName}");
        }

        var methodInfo = metadata.MethodInfo;

        return async instance =>
        {
            var methodArgs = expandedData.MethodArgumentsFactory();
            await _testInvoker.InvokeTestMethod(instance, methodInfo, methodArgs);
        };
    }

    private async Task InjectPropertiesAsync(object instance, Dictionary<string, Func<object?>> propertyFactories)
    {
        foreach (var kvp in propertyFactories)
        {
            var propertyName = kvp.Key;
            var valueFactory = kvp.Value;
            var value = valueFactory();

#pragma warning disable IL2075 // Properties come from test metadata
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
#pragma warning restore IL2075

            if (property?.CanWrite == true)
            {
                property.SetValue(instance, value);
            }
        }

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
            else if (h.MethodInfo != null)
            {
                await _hookInvoker.InvokeHookAsync(null, h.MethodInfo, context);
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
            else if (h.MethodInfo != null)
            {
                await _hookInvoker.InvokeHookAsync(instance, h.MethodInfo, context);
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
            ReturnType = test.Metadata.MethodInfo?.ReturnType ?? typeof(Task),
            ClassMetadata = CreateClassMetadata(test.Metadata),
            MethodMetadata = CreateMethodMetadata(test.Metadata)
        };

        // Add categories
        foreach (var category in test.Metadata.Categories)
        {
            testDetails.Categories.Add(category);
        }

        var context = new TestContext(test.Metadata.TestName, test.DisplayName)
        {
            TestDetails = testDetails,
            CancellationToken = CancellationToken.None
        };

        return await Task.FromResult(context);
    }

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
                Attributes = Array.Empty<AttributeMetadata>()
            }),
            Parameters = Array.Empty<ParameterMetadata>(),
            Properties = Array.Empty<PropertyMetadata>(),
            Parent = null,
            Attributes = Array.Empty<AttributeMetadata>()
        });
    }

    private static MethodMetadata CreateMethodMetadata(TestMetadata metadata)
    {
        if (metadata.MethodInfo != null)
        {
            var methodInfo = metadata.MethodInfo;
            return new MethodMetadata
            {
                Name = methodInfo.Name,
                Type = metadata.TestClassType,
                TypeReference = TypeReference.CreateConcrete(metadata.TestClassType.AssemblyQualifiedName ?? metadata.TestClassType.FullName ?? metadata.TestClassType.Name),
                Class = CreateClassMetadata(metadata),
#pragma warning disable IL2072 // Parameter types are known at compile time
                Parameters = methodInfo.GetParameters().Select(p => new ParameterMetadata(p.ParameterType)
#pragma warning restore IL2072
                {
                    Name = p.Name ?? "param" + p.Position,
                    TypeReference = TypeReference.CreateConcrete(p.ParameterType.AssemblyQualifiedName ?? p.ParameterType.FullName ?? p.ParameterType.Name),
                    Attributes = Array.Empty<AttributeMetadata>(),
                    ReflectionInfo = p
                }).ToArray(),
                GenericTypeCount = methodInfo.IsGenericMethodDefinition ? methodInfo.GetGenericArguments().Length : 0,
                ReturnTypeReference = TypeReference.CreateConcrete(methodInfo.ReturnType.AssemblyQualifiedName ?? methodInfo.ReturnType.FullName ?? methodInfo.ReturnType.Name),
                ReturnType = methodInfo.ReturnType,
                Attributes = Array.Empty<AttributeMetadata>()
            };
        }

        // Minimal metadata when MethodInfo is not available
        return new MethodMetadata
        {
            Name = metadata.TestMethodName,
            Type = metadata.TestClassType,
            TypeReference = TypeReference.CreateConcrete(metadata.TestClassType.AssemblyQualifiedName ?? metadata.TestClassType.FullName ?? metadata.TestClassType.Name),
            Class = CreateClassMetadata(metadata),
            Parameters = Array.Empty<ParameterMetadata>(),
            GenericTypeCount = 0,
            ReturnTypeReference = TypeReference.CreateConcrete(typeof(Task).AssemblyQualifiedName ?? typeof(Task).FullName ?? "System.Threading.Tasks.Task"),
            ReturnType = typeof(Task),
            Attributes = Array.Empty<AttributeMetadata>()
        };
    }
}
