using System.Runtime.CompilerServices;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Source generation mode expander that creates test variations using compile-time resolved data.
/// This implementation is 100% AOT-safe and uses only pre-generated metadata.
/// </summary>
public class SourceGenerationTestVariationExpander : ITestVariationExpander
{
    private readonly ITestNameFormatter _testNameFormatter;
    private readonly ICompileTimeDataResolver _dataResolver;

    public SourceGenerationTestVariationExpander(
        ITestNameFormatter testNameFormatter,
        ICompileTimeDataResolver dataResolver)
    {
        _testNameFormatter = testNameFormatter;
        _dataResolver = dataResolver;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TestVariation> ExpandTestVariationsAsync(
        ITestDescriptor testDescriptor, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // In source generation mode, all data sources and variations are pre-resolved at compile-time
        // We simply iterate through the pre-generated test metadata

        switch (testDescriptor)
        {
            case StaticTestDefinition staticTest:
                yield return await CreateStaticTestVariationAsync(staticTest);
                break;

            case DynamicTestMetadata dynamicTestMetadata:
                await foreach (var variation in ExpandDynamicTestAsync(dynamicTestMetadata, cancellationToken))
                {
                    yield return variation;
                }
                break;

            // Note: TestDefinition is not an ITestDescriptor, so this case is removed

            default:
                throw new NotSupportedException(
                    $"Test descriptor type {testDescriptor.GetType().Name} is not supported in source generation mode");
        }
    }

    /// <inheritdoc />
    public async Task<int> EstimateVariationCountAsync(ITestDescriptor testDescriptor)
    {
        // In source generation mode, variation count is known at compile-time
        return testDescriptor switch
        {
            StaticTestDefinition => 1,
            DynamicTestMetadata dynamicTestMetadata => await EstimateDynamicTestCountAsync(dynamicTestMetadata),
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
            ExecutionMode = TestExecutionMode.SourceGeneration,
            MethodMetadata = staticTest.TestMethodMetadata,
            ClassMetadata = staticTest.TestMethodMetadata.Class, // Use class from method metadata
            ClassArguments = Array.Empty<object?>(),
            MethodArguments = Array.Empty<object?>(),
            PropertyValues = new Dictionary<string, object?>(),
            TestFilePath = staticTest.TestFilePath,
            TestLineNumber = staticTest.TestLineNumber,
            Categories = ExtractCategories(staticTest.TestMethodMetadata),
            Attributes = staticTest.TestMethodMetadata.Attributes,
            SourceGeneratedData = new SourceGeneratedTestData
            {
                ClassInstanceFactory = () => staticTest.ClassFactory?.Invoke(Array.Empty<object?>()) ?? throw new InvalidOperationException("No class factory available"),
                // Method invoker will be resolved from source-generated registry
                MethodInvoker = null, // TODO: Get from source-generated registry
                PropertySetters = new Dictionary<string, Action<object, object?>>(),
                CompiledDataSources = Array.Empty<object?[]>()
            }
        });
    }

    private async IAsyncEnumerable<TestVariation> ExpandDynamicTestAsync(
        DynamicTestMetadata dynamicTest, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Resolve data sources at compile-time
        var classData = await _dataResolver.ResolveClassDataAsync(dynamicTest.MethodMetadata.Class);
        var methodData = await _dataResolver.ResolveMethodDataAsync(dynamicTest.MethodMetadata);
        var propertyData = await _dataResolver.ResolvePropertyDataAsync(dynamicTest.MethodMetadata.Class);

        // Handle repeat attribute
        var repeatCount = GetRepeatCount(dynamicTest.MethodMetadata);

        var testIndex = 0;

        // Generate all combinations of class data, method data, and repeats
        var classDataList = classData.Count > 0 ? classData : new List<object?[]> { Array.Empty<object?>() };
        var methodDataList = methodData.Count > 0 ? methodData : new List<object?[]> { Array.Empty<object?>() };

        foreach (var classArgs in classDataList)
        {
            var classDataIndex = classDataList.ToList().IndexOf(classArgs);
            
            foreach (var methodArgs in methodDataList)
            {
                var methodDataIndex = methodDataList.ToList().IndexOf(methodArgs);
                
                for (var repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
                {
                    var testName = _testNameFormatter.FormatTestName(
                        dynamicTest.MethodMetadata.DisplayName() ?? dynamicTest.MethodMetadata.MethodName(),
                        classArgs,
                        methodArgs,
                        propertyData);

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
                        ExecutionMode = TestExecutionMode.SourceGeneration,
                        MethodMetadata = dynamicTest.MethodMetadata,
                        ClassMetadata = dynamicTest.MethodMetadata.Class,
                        ClassArguments = classArgs,
                        MethodArguments = methodArgs,
                        PropertyValues = propertyData,
                        TestFilePath = dynamicTest.TestFilePath,
                        TestLineNumber = dynamicTest.TestLineNumber,
                        RepeatCount = repeatCount,
                        RepeatIndex = repeatIndex,
                        ClassDataIndex = classDataIndex,
                        MethodDataIndex = methodDataIndex,
                        Categories = ExtractCategories(dynamicTest.MethodMetadata),
                        Attributes = dynamicTest.MethodMetadata.Attributes,
                        SourceGeneratedData = new SourceGeneratedTestData
                        {
                            ClassInstanceFactory = () => dynamicTest.TestClassFactory?.Invoke(Array.Empty<object?>()) ?? throw new InvalidOperationException("No class factory available"),
                            MethodInvoker = null, // TODO: Get from source-generated registry
                            PropertySetters = new Dictionary<string, Action<object, object?>>(),
                            CompiledDataSources = new List<object?[]> { classArgs, methodArgs }.AsReadOnly()
                        }
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
        // Similar to dynamic test expansion but for TestDefinition
        var baseTestName = _testNameFormatter.FormatTestName(
            testDefinition.MethodMetadata.DisplayName() ?? testDefinition.MethodMetadata.MethodName());

        yield return new TestVariation
        {
            TestId = testDefinition.TestId,
            TestName = baseTestName,
            ExecutionMode = TestExecutionMode.SourceGeneration,
            MethodMetadata = testDefinition.MethodMetadata,
            ClassMetadata = testDefinition.MethodMetadata.Class,
            ClassArguments = Array.Empty<object?>(),
            MethodArguments = Array.Empty<object?>(),
            PropertyValues = testDefinition.PropertiesProvider(),
            TestFilePath = testDefinition.TestFilePath,
            TestLineNumber = testDefinition.TestLineNumber,
            Categories = ExtractCategories(testDefinition.MethodMetadata),
            Attributes = testDefinition.MethodMetadata.Attributes,
            SourceGeneratedData = new SourceGeneratedTestData
            {
                ClassInstanceFactory = testDefinition.TestClassFactory,
                MethodInvoker = null, // TODO: Get from source-generated registry
                PropertySetters = new Dictionary<string, Action<object, object?>>(),
                CompiledDataSources = Array.Empty<object?[]>()
            }
        };
    }

    private async Task<int> EstimateDynamicTestCountAsync(DynamicTestMetadata dynamicTest)
    {
        // Get data source counts from compile-time resolution
        var classData = await _dataResolver.ResolveClassDataAsync(dynamicTest.MethodMetadata.Class);
        var methodData = await _dataResolver.ResolveMethodDataAsync(dynamicTest.MethodMetadata);
        var repeatCount = GetRepeatCount(dynamicTest.MethodMetadata);

        var classDataCount = Math.Max(1, classData.Count);
        var methodDataCount = Math.Max(1, methodData.Count);

        return classDataCount * methodDataCount * repeatCount;
    }

    private static int EstimateTestDefinitionCount(TestDefinition testDefinition)
    {
        // TestDefinition typically produces one variation
        return 1;
    }

    private static int GetRepeatCount(MethodMetadata methodMetadata)
    {
        var repeatAttribute = methodMetadata.GetAttribute<RepeatAttribute>();
        return Math.Max(1, repeatAttribute?.Times ?? 1);
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
}