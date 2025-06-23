using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;
using TUnit.Core.Extensions;

namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of test metadata expander.
/// Expands test metadata into executable test definitions by enumerating data sources.
/// </summary>
public class TestMetadataExpander : ITestMetadataExpander
{
    private readonly IDataProviderService _dataProviderService;
    private readonly ITestNameFormatter _testNameFormatter;
    private readonly ITestInstanceFactory _testInstanceFactory;

    public TestMetadataExpander(
        IDataProviderService dataProviderService,
        ITestNameFormatter testNameFormatter,
        ITestInstanceFactory testInstanceFactory)
    {
        _dataProviderService = dataProviderService;
        _testNameFormatter = testNameFormatter;
        _testInstanceFactory = testInstanceFactory;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TestDefinition>> ExpandTestsAsync(
        ITestDescriptor metadata,
        CancellationToken cancellationToken = default)
    {
        return metadata switch
        {
            StaticTestDefinition staticDef => await ExpandStaticTestAsync(staticDef, cancellationToken),
            DynamicTestMetadata dynamicMeta => await ExpandDynamicTestAsync(dynamicMeta, cancellationToken),
            TestDefinition testDef => new[] { testDef }, // Already a TestDefinition, return as-is
            _ => throw new NotSupportedException($"Unsupported test descriptor type: {metadata.GetType().Name}")
        };
    }

    private async Task<IEnumerable<TestDefinition>> ExpandStaticTestAsync(
        StaticTestDefinition staticDef,
        CancellationToken cancellationToken)
    {
        // StaticTestDefinition already provides all the data we need
        // Just create TestDefinition(s) based on it
        
        // Create a single TestDefinition that will be expanded by TestBuilder
        var definition = new TestDefinition
        {
            TestId = staticDef.TestId,
            MethodMetadata = staticDef.TestMethodMetadata,
            TestFilePath = staticDef.TestFilePath,
            TestLineNumber = staticDef.TestLineNumber,
            TestClassFactory = () => staticDef.ClassFactory(Array.Empty<object?>()),
            TestMethodInvoker = async (instance, ct) => await staticDef.MethodInvoker(instance, Array.Empty<object?>(), ct),
            PropertiesProvider = () => staticDef.PropertyValuesProvider().FirstOrDefault() ?? new Dictionary<string, object?>(),
            ClassDataProvider = staticDef.ClassDataProvider,
            MethodDataProvider = staticDef.MethodDataProvider,
            OriginalClassFactory = staticDef.ClassFactory,
            OriginalMethodInvoker = staticDef.MethodInvoker
        };

        return await Task.FromResult(new[] { definition });
    }

    [RequiresDynamicCode("Dynamic test expansion requires runtime type resolution")]
    [RequiresUnreferencedCode("Dynamic test expansion may use types that aren't statically referenced")]
    private async Task<IEnumerable<TestDefinition>> ExpandDynamicTestAsync(
        DynamicTestMetadata dynamicMeta,
        CancellationToken cancellationToken)
    {
        // Resolve the test class type if needed
        var testClassType = dynamicMeta.TestClassType ?? 
            ResolveTypeReference(dynamicMeta.TestClassTypeReference);

        // Create data providers from the data sources
        var classDataProvider = CreateCompositeDataProvider(dynamicMeta.ClassDataSources);
        var methodDataProvider = CreateCompositeDataProvider(dynamicMeta.MethodDataSources);

        // Create property provider
        var propertyProvider = await CreatePropertyProviderAsync(
            dynamicMeta.PropertyDataSources, 
            cancellationToken);

        // Create a factory function that uses the test class factory
        Func<object?[], object> classFactory = dynamicMeta.TestClassFactory ?? 
            (args => CreateTestInstance(testClassType, args));

        // Create the test definition
        var definition = new TestDefinition
        {
            TestId = dynamicMeta.TestIdTemplate,
            MethodMetadata = dynamicMeta.MethodMetadata,
            TestFilePath = dynamicMeta.TestFilePath,
            TestLineNumber = dynamicMeta.TestLineNumber,
            TestClassFactory = () => classFactory(Array.Empty<object?>()),
            TestMethodInvoker = CreateMethodInvoker(dynamicMeta.MethodMetadata),
            PropertiesProvider = propertyProvider,
            ClassDataProvider = classDataProvider,
            MethodDataProvider = methodDataProvider,
            OriginalClassFactory = classFactory,
            OriginalMethodInvoker = CreateOriginalMethodInvoker(dynamicMeta.MethodMetadata)
        };

        return await Task.FromResult(new[] { definition });
    }

    private IDataProvider CreateCompositeDataProvider(IReadOnlyList<IDataSourceProvider> providers)
    {
        if (!providers.Any())
        {
            return new EmptyDataProvider();
        }

        if (providers.Count == 1)
        {
            return new DataSourceProviderAdapter(providers[0]);
        }

        return new CompositeDataProvider(providers.Select(p => new DataSourceProviderAdapter(p)).ToList());
    }

    private async Task<Func<IDictionary<string, object?>>> CreatePropertyProviderAsync(
        IReadOnlyDictionary<PropertyInfo, IDataSourceProvider> propertyDataSources,
        CancellationToken cancellationToken)
    {
        if (!propertyDataSources.Any())
        {
            return () => new Dictionary<string, object?>();
        }

        // Pre-fetch property values
        var propertyValues = new Dictionary<string, object?>();
        foreach (var (property, dataSource) in propertyDataSources)
        {
            var data = await _dataProviderService
                .GetTestDataAsync(new[] { dataSource }, cancellationToken)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (data?.Length > 0)
            {
                propertyValues[property.Name] = data[0];
            }
        }

        return () => propertyValues;
    }

    private object CreateTestInstance(Type type, object?[] args)
    {
        // Use the instance factory service synchronously for now
        // This will be improved when we fix async/await issues
        return _testInstanceFactory.CreateInstanceAsync(type, args).Result;
    }

    private Func<object, CancellationToken, ValueTask> CreateMethodInvoker(MethodMetadata methodMetadata)
    {
        var methodInfo = methodMetadata.ReflectionInformation as MethodInfo;
        if (methodInfo == null)
        {
            throw new InvalidOperationException(
                $"Method {methodMetadata.Name} does not have reflection information available.");
        }

        return async (instance, cancellationToken) =>
        {
            await _testInstanceFactory.InvokeMethodAsync(instance, methodInfo, Array.Empty<object?>());
        };
    }

    private Func<object, object?[], CancellationToken, Task> CreateOriginalMethodInvoker(MethodMetadata methodMetadata)
    {
        var methodInfo = methodMetadata.ReflectionInformation as MethodInfo;
        if (methodInfo == null)
        {
            throw new InvalidOperationException(
                $"Method {methodMetadata.Name} does not have reflection information available.");
        }

        return async (instance, args, cancellationToken) =>
        {
            await _testInstanceFactory.InvokeMethodAsync(instance, methodInfo, args);
        };
    }

    [RequiresUnreferencedCode("Type resolution may require types that aren't statically referenced")]
    private Type ResolveTypeReference(TypeReference typeRef)
    {
        // For now, simple implementation that assumes the type can be loaded
        var typeName = typeRef.FullName;
        var type = Type.GetType(typeName);
        
        if (type == null)
        {
            throw new InvalidOperationException(
                $"Could not resolve type reference: {typeName}");
        }

        return type;
    }

    /// <summary>
    /// Adapter to wrap IDataSourceProvider as IDataProvider
    /// </summary>
    private class DataSourceProviderAdapter : IDataProvider
    {
        private readonly IDataSourceProvider _provider;

        public DataSourceProviderAdapter(IDataSourceProvider provider)
        {
            _provider = provider;
        }

        public async Task<IEnumerable<object?[]>> GetData()
        {
            if (_provider.IsAsync)
            {
                var results = new List<object?[]>();
                await foreach (var data in _provider.GetDataAsync())
                {
                    results.Add(data);
                }
                return results;
            }

            return _provider.GetData();
        }
    }

    /// <summary>
    /// Empty data provider that returns no data
    /// </summary>
    private class EmptyDataProvider : IDataProvider
    {
        public Task<IEnumerable<object?[]>> GetData()
        {
            return Task.FromResult(Enumerable.Empty<object?[]>());
        }
    }

    /// <summary>
    /// Composite data provider that combines multiple providers
    /// </summary>
    private class CompositeDataProvider : IDataProvider
    {
        private readonly List<IDataProvider> _providers;

        public CompositeDataProvider(List<IDataProvider> providers)
        {
            _providers = providers;
        }

        public async Task<IEnumerable<object?[]>> GetData()
        {
            var allData = new List<object?[]>();
            foreach (var provider in _providers)
            {
                var data = await provider.GetData();
                allData.AddRange(data);
            }
            return allData;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ExpandedTest> ExpandTestAsync(
        ITestDescriptor testDescriptor,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // First get the test definitions
        var definitions = await ExpandTestsAsync(testDescriptor, cancellationToken);
        
        // Then expand each definition into ExpandedTest instances
        foreach (var definition in definitions)
        {
            await foreach (var expandedTest in ExpandFromDefinitionAsync(definition, cancellationToken))
            {
                yield return expandedTest;
            }
        }
    }

    private async IAsyncEnumerable<ExpandedTest> ExpandFromDefinitionAsync(
        TestDefinition definition,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get all test data combinations from data providers
        var classDataRows = await definition.ClassDataProvider.GetData();
        var methodDataRows = await definition.MethodDataProvider.GetData();
        
        // If no data rows, create single test with empty arguments
        var classArgsList = classDataRows.Any() ? classDataRows.ToList() : new List<object?[]> { Array.Empty<object?>() };
        var methodArgsList = methodDataRows.Any() ? methodDataRows.ToList() : new List<object?[]> { Array.Empty<object?>() };
        
        // Get property values if available
        var propertyValues = definition.PropertiesProvider();
        
        // Get repeat count
        var repeatCount = definition.MethodMetadata.GetAttribute<RepeatAttribute>()?.Times ?? 0;
        
        var testIndex = 0;
        
        // Generate test for each combination of class and method data
        foreach (var classArgs in classArgsList)
        {
            foreach (var methodArgs in methodArgsList)
            {
                for (var repeatIndex = 0; repeatIndex <= repeatCount; repeatIndex++)
                {
                    var testId = _testNameFormatter.BuildTestId(
                        definition.TestId,
                        testIndex,
                        repeatIndex,
                        classArgsList.IndexOf(classArgs),
                        methodArgsList.IndexOf(methodArgs));
                    
                    var testName = _testNameFormatter.FormatTestName(
                        definition.MethodMetadata.Name,
                        classArgs,
                        methodArgs,
                        propertyValues);
                    
                    // Create test instance
                    var testInstance = definition.OriginalClassFactory != null 
                        ? definition.OriginalClassFactory(classArgs)
                        : definition.TestClassFactory();
                    
                    // Get method info
                    var methodInfo = definition.MethodMetadata.ReflectionInformation as MethodInfo;
                    if (methodInfo == null)
                    {
                        throw new InvalidOperationException(
                            $"Method {definition.MethodMetadata.Name} does not have reflection information.");
                    }
                    
                    // Get timeout from attributes
                    var timeout = definition.MethodMetadata.GetAttribute<TimeoutAttribute>()?.Duration;
                    
                    // Check if test is skipped
                    var skipAttribute = definition.MethodMetadata.GetAttribute<SkipAttribute>();
                    
                    yield return new ExpandedTest
                    {
                        TestId = testId,
                        TestName = testName,
                        TestInstance = testInstance,
                        ClassArguments = classArgs,
                        MethodArguments = methodArgs,
                        PropertyValues = propertyValues,
                        MethodMetadata = definition.MethodMetadata,
                        TestMethod = methodInfo,
                        TestFilePath = definition.TestFilePath,
                        TestLineNumber = definition.TestLineNumber,
                        Timeout = timeout,
                        IsSkipped = skipAttribute != null,
                        SkipReason = skipAttribute?.Reason
                    };
                    
                    testIndex++;
                }
            }
        }
    }
}