using System.Diagnostics;
using System.Reflection;
using TUnit.Core.Diagnostics;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// TestBuilder with integrated diagnostics for debugging and performance analysis.
/// </summary>
public class TestBuilderWithDiagnostics
{
    private readonly TestBuilderDiagnostics _diagnostics;
    private readonly TestBuilderOptimized _optimizedBuilder;
    
    public TestBuilderWithDiagnostics(TestBuilderDiagnostics? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new TestBuilderDiagnostics();
        _optimizedBuilder = new TestBuilderOptimized();
    }
    
    /// <summary>
    /// Gets the diagnostics instance for this builder.
    /// </summary>
    public TestBuilderDiagnostics Diagnostics => _diagnostics;
    
    /// <summary>
    /// Builds all test definitions from the given metadata with diagnostic tracking.
    /// </summary>
    public async Task<IEnumerable<TestDefinition>> BuildTestsAsync(TestMetadata metadata, CancellationToken cancellationToken = default)
    {
        using var mainScope = _diagnostics.BeginScope("BuildTestsAsync",
            ("TestClass", metadata.TestClassType.Name),
            ("TestMethod", metadata.TestMethod.Name));
        
        _diagnostics.LogTestMetadata(metadata);
        
        // Use optimized builder for actual work
        if (!_diagnostics.IsEnabled)
        {
            return await _optimizedBuilder.BuildTestsAsync(metadata, cancellationToken);
        }
        
        // Run with full diagnostics
        var testDefinitions = new List<TestDefinition>();
        
        try
        {
            // Get all combinations of class and method data
            TestCombination[] testCombinations;
            using (var combScope = _diagnostics.BeginScope("GetTestCombinations"))
            {
                testCombinations = await GetTestCombinationsWithDiagnosticsAsync(metadata, cancellationToken);
            }
            
            _diagnostics.LogCombinationGeneration(
                metadata.ClassDataSources.Count,
                metadata.MethodDataSources.Count,
                testCombinations.Length);
            
            var testIndex = 0;
            using var buildScope = _diagnostics.BeginScope("BuildTestDefinitions",
                ("CombinationCount", testCombinations.Length),
                ("RepeatCount", metadata.RepeatCount));
            
            foreach (var combination in testCombinations)
            {
                for (var repeatIndex = 0; repeatIndex < metadata.RepeatCount; repeatIndex++)
                {
                    using var testScope = _diagnostics.BeginScope($"BuildTest_{testIndex}");
                    
                    var testDefinition = await BuildSingleTestDefinitionWithDiagnosticsAsync(
                        metadata, 
                        combination, 
                        testIndex, 
                        repeatIndex,
                        cancellationToken);
                    
                    if (testDefinition != null)
                    {
                        testDefinitions.Add(testDefinition);
                        _diagnostics.Log($"Built test definition: {testDefinition.TestId}", DiagnosticLevel.Debug);
                    }
                    
                    testIndex++;
                }
            }
            
            _diagnostics.Log($"Successfully built {testDefinitions.Count} test definitions", DiagnosticLevel.Info);
            return testDefinitions;
        }
        catch (Exception ex)
        {
            _diagnostics.LogError("BuildTestsAsync", ex);
            throw;
        }
    }
    
    private async Task<TestCombination[]> GetTestCombinationsWithDiagnosticsAsync(
        TestMetadata metadata, 
        CancellationToken cancellationToken)
    {
        var combinations = new List<TestCombination>();
        
        // Get all class data combinations
        List<object?[]> classDataSets;
        using (var scope = _diagnostics.BeginScope("GetClassDataSets"))
        {
            var sw = Stopwatch.StartNew();
            classDataSets = await GetDataSetsAsync(metadata.ClassDataSources, "Class", cancellationToken);
            _diagnostics.LogDataSourceEnumeration("ClassDataSources", classDataSets.Count, sw.Elapsed);
        }
        
        // Get all method data combinations
        List<object?[]> methodDataSets;
        using (var scope = _diagnostics.BeginScope("GetMethodDataSets"))
        {
            var sw = Stopwatch.StartNew();
            methodDataSets = await GetDataSetsAsync(metadata.MethodDataSources, "Method", cancellationToken);
            _diagnostics.LogDataSourceEnumeration("MethodDataSources", methodDataSets.Count, sw.Elapsed);
        }
        
        // Get all property data combinations
        var propertyDataSets = new Dictionary<PropertyInfo, List<object?[]>>();
        using (var scope = _diagnostics.BeginScope("GetPropertyDataSets"))
        {
            foreach (var (property, dataSource) in metadata.PropertyDataSources)
            {
                var sw = Stopwatch.StartNew();
                var dataSets = await GetDataSetsAsync(new[] { dataSource }, $"Property_{property.Name}", cancellationToken);
                propertyDataSets[property] = dataSets;
                _diagnostics.LogDataSourceEnumeration($"Property_{property.Name}", dataSets.Count, sw.Elapsed);
            }
        }
        
        // Generate all combinations
        if (!classDataSets.Any())
            classDataSets.Add(Array.Empty<object?>());
        if (!methodDataSets.Any())
            methodDataSets.Add(Array.Empty<object?>());
        
        using (var scope = _diagnostics.BeginScope("GenerateCombinations"))
        {
            foreach (var classData in classDataSets)
            {
                foreach (var methodData in methodDataSets)
                {
                    var propertyValues = new Dictionary<PropertyInfo, object?>();
                    
                    foreach (var (property, dataSets) in propertyDataSets)
                    {
                        if (dataSets.Any())
                        {
                            var data = dataSets.First();
                            if (data.Length > 0)
                            {
                                propertyValues[property] = data[0];
                            }
                        }
                    }
                    
                    combinations.Add(new TestCombination
                    {
                        ClassArguments = classData,
                        MethodArguments = methodData,
                        PropertyValues = propertyValues
                    });
                }
            }
        }
        
        return combinations.ToArray();
    }
    
    private async Task<List<object?[]>> GetDataSetsAsync(
        IEnumerable<IDataSourceProvider> dataSourceProviders,
        string sourceName,
        CancellationToken cancellationToken)
    {
        var allDataSets = new List<object?[]>();
        
        foreach (var provider in dataSourceProviders)
        {
            try
            {
                if (provider.IsAsync)
                {
                    await foreach (var data in provider.GetDataAsync().WithCancellation(cancellationToken))
                    {
                        allDataSets.Add(data);
                    }
                }
                else
                {
                    foreach (var data in provider.GetData())
                    {
                        allDataSets.Add(data);
                    }
                }
            }
            catch (Exception ex)
            {
                _diagnostics.LogError($"GetDataSets({sourceName})", ex);
                throw;
            }
        }
        
        return allDataSets;
    }
    
    private async Task<TestDefinition?> BuildSingleTestDefinitionWithDiagnosticsAsync(
        TestMetadata metadata,
        TestCombination combination,
        int testIndex,
        int repeatIndex,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build test ID from template
            var testId = BuildTestId(metadata.TestIdTemplate, testIndex, repeatIndex);
            _diagnostics.Log($"Building test: {testId}", DiagnosticLevel.Debug);
            
            // Unwrap tuples if necessary
            object?[] unwrappedClassArgs;
            object?[] unwrappedMethodArgs;
            
            using (var unwrapScope = _diagnostics.BeginScope("UnwrapTuples"))
            {
                unwrappedClassArgs = UnwrapTuplesWithDiagnostics(combination.ClassArguments, "ClassArgs");
                unwrappedMethodArgs = UnwrapTuplesWithDiagnostics(combination.MethodArguments, "MethodArgs");
            }
            
            // Create factories that capture the current combination
            var classFactory = CreateClassFactory(metadata, unwrappedClassArgs, combination.PropertyValues);
            var methodInvoker = CreateMethodInvoker(metadata, unwrappedMethodArgs);
            var propertiesProvider = CreatePropertiesProvider(combination.PropertyValues);
            
            return new TestDefinition
            {
                TestId = testId,
                MethodMetadata = metadata.MethodMetadata,
                TestFilePath = metadata.TestFilePath,
                TestLineNumber = metadata.TestLineNumber,
                TestClassFactory = classFactory,
                TestMethodInvoker = methodInvoker,
                ClassArgumentsProvider = () => unwrappedClassArgs,
                MethodArgumentsProvider = () => unwrappedMethodArgs,
                PropertiesProvider = propertiesProvider
            };
        }
        catch (Exception ex)
        {
            _diagnostics.LogError($"BuildSingleTestDefinition(Index={testIndex}, Repeat={repeatIndex})", ex);
            return null;
        }
    }
    
    private object?[] UnwrapTuplesWithDiagnostics(object?[] arguments, string context)
    {
        if (arguments.Length == 1 && arguments[0] != null)
        {
            var argType = arguments[0].GetType();
            if (IsTupleType(argType))
            {
                var result = UnwrapTuple(arguments[0]);
                _diagnostics.LogTupleUnwrapping(argType, result.Length);
                return result;
            }
        }
        
        return arguments;
    }
    
    // Helper methods (simplified versions, actual implementation would use optimized versions)
    
    private string BuildTestId(string template, int testIndex, int repeatIndex)
    {
        return template
            .Replace("{TestIndex}", testIndex.ToString())
            .Replace("{RepeatIndex}", repeatIndex.ToString());
    }
    
    private bool IsTupleType(Type type)
    {
        return type.IsGenericType && 
               type.FullName?.StartsWith("System.ValueTuple`") == true;
    }
    
    private object?[] UnwrapTuple(object tuple)
    {
        var tupleType = tuple.GetType();
        var fields = tupleType.GetFields();
        var values = new List<object?>();
        
        foreach (var field in fields.Where(f => f.Name.StartsWith("Item")))
        {
            var value = field.GetValue(tuple);
            
            if (field.Name == "Rest" && value != null && IsTupleType(value.GetType()))
            {
                values.AddRange(UnwrapTuple(value));
            }
            else
            {
                values.Add(value);
            }
        }
        
        return values.ToArray();
    }
    
    private Func<object> CreateClassFactory(
        TestMetadata metadata, 
        object?[] classArgs,
        Dictionary<PropertyInfo, object?> propertyValues)
    {
        return () =>
        {
            using var scope = _diagnostics.BeginScope("CreateClassInstance",
                ("Type", metadata.TestClassType.Name));
            
            var instance = metadata.TestClassFactory(classArgs);
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to create instance of {metadata.TestClassType.Name}");
            }
            
            // Initialize properties
            foreach (var (property, value) in propertyValues)
            {
                try
                {
                    if (value is IAsyncInitializer asyncInitializer)
                    {
                        Task.Run(async () => await ObjectInitializer.InitializeAsync(asyncInitializer)).GetAwaiter().GetResult();
                    }
                    
                    property.SetValue(instance, value);
                    _diagnostics.Log($"Set property {property.Name} = {value}", DiagnosticLevel.Debug);
                }
                catch (Exception ex)
                {
                    _diagnostics.LogError($"SetProperty({property.Name})", ex);
                    throw new InvalidOperationException(
                        $"Failed to set property {property.Name} on {metadata.TestClassType.Name}", ex);
                }
            }
            
            return instance;
        };
    }
    
    private Func<object, CancellationToken, ValueTask> CreateMethodInvoker(
        TestMetadata metadata, 
        object?[] methodArgs)
    {
        return async (instance, cancellationToken) =>
        {
            var result = metadata.TestMethod.Invoke(instance, methodArgs);
            
            if (result is Task task)
            {
                await task;
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask;
            }
        };
    }
    
    private Func<IDictionary<string, object?>> CreatePropertiesProvider(
        Dictionary<PropertyInfo, object?> propertyValues)
    {
        var properties = new Dictionary<string, object?>();
        
        foreach (var (property, value) in propertyValues)
        {
            properties[property.Name] = value;
        }
        
        return () => properties;
    }
    
    private class TestCombination
    {
        public required object?[] ClassArguments { get; init; }
        public required object?[] MethodArguments { get; init; }
        public required Dictionary<PropertyInfo, object?> PropertyValues { get; init; }
    }
}