using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Reflection mode expander that creates test variations using runtime data source evaluation.
/// This implementation provides full flexibility but requires dynamic code generation capabilities.
/// </summary>
[RequiresDynamicCode("Reflection mode test expansion requires runtime type generation")]
[RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
public class ReflectionTestVariationExpander : ITestVariationExpander
{
    private readonly IDataProviderService _dataProviderService;
    private readonly ITestNameFormatter _testNameFormatter;
    private readonly ITestInstanceFactory _testInstanceFactory;

    public ReflectionTestVariationExpander(
        IDataProviderService dataProviderService,
        ITestNameFormatter testNameFormatter,
        ITestInstanceFactory testInstanceFactory)
    {
        _dataProviderService = dataProviderService;
        _testNameFormatter = testNameFormatter;
        _testInstanceFactory = testInstanceFactory;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TestVariation> ExpandTestVariationsAsync(
        ITestDescriptor testDescriptor, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        switch (testDescriptor)
        {
            case StaticTestDefinition staticTest:
                yield return await CreateStaticTestVariationAsync(staticTest);
                break;

            case DynamicTest dynamicTest:
                await foreach (var variation in ExpandDynamicTestAsync(dynamicTest, cancellationToken))
                {
                    yield return variation;
                }
                break;

            // Note: TestDefinition is not an ITestDescriptor, so this case is removed

            default:
                throw new NotSupportedException(
                    $"Test descriptor type {testDescriptor.GetType().Name} is not supported in reflection mode");
        }
    }

    /// <inheritdoc />
    public async Task<int> EstimateVariationCountAsync(ITestDescriptor testDescriptor)
    {
        return testDescriptor switch
        {
            StaticTestDefinition => 1,
            DynamicTest dynamicTest => await EstimateDynamicTestCountAsync(dynamicTest),
            // Note: TestDefinition is not an ITestDescriptor, so this case is removed
            _ => 1
        };
    }

    private Task<TestVariation> CreateStaticTestVariationAsync(StaticTestDefinition staticTest)
    {
        var testName = _testNameFormatter.FormatTestName(
            staticTest.TestMethodMetadata.DisplayName() ?? staticTest.TestMethodMetadata.MethodName());

        return Task.FromResult(new TestVariation
        {
            TestId = staticTest.TestId,
            TestName = testName,
            ExecutionMode = TestExecutionMode.Reflection,
            MethodMetadata = staticTest.TestMethodMetadata,
            ClassMetadata = staticTest.TestMethodMetadata.Class, // Use class from method metadata
            ClassArguments = Array.Empty<object?>(),
            MethodArguments = Array.Empty<object?>(),
            PropertyValues = new Dictionary<string, object?>(),
            TestFilePath = staticTest.TestFilePath,
            TestLineNumber = staticTest.TestLineNumber,
            Categories = ExtractCategories(staticTest.TestMethodMetadata),
            Attributes = staticTest.TestMethodMetadata.Attributes
        });
    }

    private async IAsyncEnumerable<TestVariation> ExpandDynamicTestAsync(
        DynamicTest dynamicTest, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Create metadata from dynamic test properties
        var classMetadata = CreateClassMetadataFromDynamicTest(dynamicTest);
        var methodMetadata = CreateMethodMetadataFromDynamicTest(dynamicTest);

        // Get all data sources for this test
        var classDataSources = await GetClassDataSourcesAsync(classMetadata);
        var methodDataSources = await GetMethodDataSourcesAsync(methodMetadata);
        var propertyDataSources = await GetPropertyDataSourcesAsync(classMetadata);

        // Handle repeat attribute
        var repeatCount = GetRepeatCount(methodMetadata);

        var testIndex = 0;
        
        // Generate all combinations of class data, method data, and repeats
        for (var classDataIndex = 0; classDataIndex < Math.Max(1, classDataSources.Count); classDataIndex++)
        {
            var classArgs = classDataIndex < classDataSources.Count ? classDataSources[classDataIndex] : Array.Empty<object?>();
            
            for (var methodDataIndex = 0; methodDataIndex < Math.Max(1, methodDataSources.Count); methodDataIndex++)
            {
                var methodArgs = methodDataIndex < methodDataSources.Count ? methodDataSources[methodDataIndex] : Array.Empty<object?>();
                
                for (var repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
                {
                    var testName = _testNameFormatter.FormatTestName(
                        methodMetadata.DisplayName() ?? methodMetadata.MethodName(),
                        classArgs,
                        methodArgs,
                        propertyDataSources);

                    var testId = _testNameFormatter.BuildTestId(
                        dynamicTest.TestId,
                        testIndex++,
                        repeatIndex,
                        classDataIndex,
                        methodDataIndex);

                    yield return new TestVariation
                    {
                        TestId = testId,
                        TestName = testName,
                        ExecutionMode = TestExecutionMode.Reflection,
                        MethodMetadata = methodMetadata,
                        ClassMetadata = classMetadata,
                        ClassArguments = classArgs,
                        MethodArguments = methodArgs,
                        PropertyValues = propertyDataSources,
                        TestFilePath = dynamicTest.TestFilePath,
                        TestLineNumber = dynamicTest.TestLineNumber,
                        RepeatCount = repeatCount,
                        RepeatIndex = repeatIndex,
                        ClassDataIndex = classDataIndex,
                        MethodDataIndex = methodDataIndex,
                        Categories = ExtractCategories(methodMetadata),
                        Attributes = methodMetadata.Attributes
                    };
                }
            }
        }
    }

#pragma warning disable CS1998 // Async method lacks await
    private async IAsyncEnumerable<TestVariation> ExpandTestDefinitionAsync(
        TestDefinition testDefinition, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Similar expansion logic for TestDefinition
        var testName = _testNameFormatter.FormatTestName(
            testDefinition.MethodMetadata.DisplayName() ?? testDefinition.MethodMetadata.MethodName());

        yield return new TestVariation
        {
            TestId = testDefinition.TestId,
            TestName = testName,
            ExecutionMode = TestExecutionMode.Reflection,
            MethodMetadata = testDefinition.MethodMetadata,
            ClassMetadata = testDefinition.MethodMetadata.Class, // Use class from method metadata
            ClassArguments = Array.Empty<object?>(),
            MethodArguments = Array.Empty<object?>(),
            PropertyValues = testDefinition.PropertiesProvider(),
            TestFilePath = testDefinition.TestFilePath,
            TestLineNumber = testDefinition.TestLineNumber,
            Categories = ExtractCategories(testDefinition.MethodMetadata),
            Attributes = testDefinition.MethodMetadata.Attributes
        };
    }

#pragma warning disable CS1998 // Async method lacks await
    private async Task<IReadOnlyList<object?[]>> GetClassDataSourcesAsync(ClassMetadata classMetadata)
    {
        // In reflection mode, we need to evaluate data attributes at runtime
        // This is a simplified implementation - real implementation would need to
        // convert IDataAttribute instances to IDataProvider instances
        var dataSources = new List<object?[]>();
        var dataAttributes = classMetadata.GetDataAttributes();

        foreach (var dataAttribute in dataAttributes)
        {
            // For now, return empty data - this needs proper implementation
            // to convert IDataAttribute to IDataProvider
            if (dataAttribute is ArgumentsAttribute argsAttr)
            {
                dataSources.Add(argsAttr.Values);
            }
        }

        return dataSources.AsReadOnly();
    }

    private async Task<IReadOnlyList<object?[]>> GetMethodDataSourcesAsync(MethodMetadata methodMetadata)
    {
        var dataSources = new List<object?[]>();
        var dataAttributes = methodMetadata.GetDataAttributes();

        foreach (var dataAttribute in dataAttributes)
        {
            // TODO: Convert IDataAttribute to IDataSourceProvider
            // This requires runtime evaluation of data attributes
            if (dataAttribute is ArgumentsAttribute argsAttr)
            {
                dataSources.Add(argsAttr.Values);
            }
        }

        return await Task.FromResult(dataSources.AsReadOnly());
    }

    private Task<IDictionary<string, object?>> GetPropertyDataSourcesAsync(ClassMetadata classMetadata)
    {
        var propertyValues = new Dictionary<string, object?>();
        
        // This would evaluate property data sources at runtime
        // Implementation depends on property injection system
        
        return Task.FromResult<IDictionary<string, object?>>(propertyValues);
    }

    private static int GetRepeatCount(MethodMetadata methodMetadata)
    {
        var repeatAttribute = methodMetadata.GetAttribute<RepeatAttribute>();
        return repeatAttribute?.Times ?? 1;
    }

    private Task<int> EstimateDynamicTestCountAsync(DynamicTest dynamicTest)
    {
        // DynamicTest doesn't have the properties we need
        // Return a simple estimate for now
        return Task.FromResult(1);
    }

    private Task<int> EstimateTestDefinitionCountAsync(TestDefinition testDefinition)
    {
        // For TestDefinition, typically just one variation
        return Task.FromResult(1);
    }

    private static IReadOnlyList<string> ExtractCategories(MethodMetadata methodMetadata)
    {
        var categories = new List<string>();
        var categoryAttributes = methodMetadata.GetAttributes<CategoryAttribute>();
        
        foreach (var categoryAttribute in categoryAttributes)
        {
            categories.Add(categoryAttribute.Category);
        }

        return categories.AsReadOnly();
    }

    private static ClassMetadata CreateClassMetadataFromDynamicTest(DynamicTest dynamicTest)
    {
        // Create a basic ClassMetadata from DynamicTest properties
        // This is a simplified implementation - in a real scenario, this would need
        // to be more comprehensive
        var assemblyMetadata = new AssemblyMetadata
        {
            Name = dynamicTest.TestClassType.Assembly.FullName ?? dynamicTest.TestClassType.Assembly.GetName().Name ?? "Unknown",
            Attributes = Array.Empty<AttributeMetadata>()
        };

        return new ClassMetadata
        {
            Type = dynamicTest.TestClassType,
            Name = dynamicTest.TestClassType.Name,
            Namespace = dynamicTest.TestClassType.Namespace,
            Assembly = assemblyMetadata,
            Attributes = dynamicTest.Attributes.Select(a => new AttributeMetadata { Instance = a, TargetElement = TestAttributeTarget.Class }).ToArray(),
            Parameters = Array.Empty<ParameterMetadata>(),
            Properties = Array.Empty<PropertyMetadata>(),
            Parent = null,
            TypeReference = TypeReference.CreateConcrete(dynamicTest.TestClassType.AssemblyQualifiedName ?? "")
        };
    }

    private static MethodMetadata CreateMethodMetadataFromDynamicTest(DynamicTest dynamicTest)
    {
        var classMetadata = CreateClassMetadataFromDynamicTest(dynamicTest);
        var method = dynamicTest.TestBody;

        return new MethodMetadata
        {
            Type = dynamicTest.TestClassType,
            Name = method.Name,
            Class = classMetadata,
            Attributes = method.GetCustomAttributes(true).Select(a => new AttributeMetadata { Instance = (Attribute)a, TargetElement = TestAttributeTarget.Method }).ToArray(),
            Parameters = method.GetParameters().Select(p => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? "",
                Attributes = Array.Empty<AttributeMetadata>(),
                TypeReference = TypeReference.CreateConcrete(p.ParameterType.AssemblyQualifiedName ?? ""),
                ReflectionInfo = p
            }).ToArray(),
            GenericTypeCount = method.GetGenericArguments().Length,
            ReturnType = method.ReturnType,
            ReturnTypeReference = TypeReference.CreateConcrete(method.ReturnType.AssemblyQualifiedName ?? ""),
            TypeReference = TypeReference.CreateConcrete(dynamicTest.TestClassType.AssemblyQualifiedName ?? "")
        };
    }
}