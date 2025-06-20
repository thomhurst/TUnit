using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Optimized version of TestBuilder with caching and expression compilation for better performance.
/// </summary>
public class TestBuilderOptimized
{
    // Caches for reflection operations
    private static readonly ConcurrentDictionary<Type, ConstructorInfo?> ConstructorCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyCache = new();
    private static readonly ConcurrentDictionary<Type, bool> TupleTypeCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, Func<object?, object?[], object?>> MethodInvokerCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> PropertySetterCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object?[], object>> ConstructorInvokerCache = new();
    
    /// <summary>
    /// Builds all test definitions from the given metadata with optimized performance.
    /// </summary>
    public async Task<IEnumerable<TestDefinition>> BuildTestsAsync(TestMetadata metadata, CancellationToken cancellationToken = default)
    {
        var testDefinitions = new List<TestDefinition>();
        
        // Pre-compile delegates for this metadata
        var compiledFactories = CompileFactories(metadata);
        
        // Get all combinations of class and method data
        var testCombinations = await GetTestCombinationsAsync(metadata, cancellationToken);
        
        var testIndex = 0;
        foreach (var combination in testCombinations)
        {
            // Handle repeat count
            for (var repeatIndex = 0; repeatIndex < metadata.RepeatCount; repeatIndex++)
            {
                var testDefinition = await BuildSingleTestDefinitionAsync(
                    metadata, 
                    combination, 
                    testIndex, 
                    repeatIndex,
                    compiledFactories,
                    cancellationToken);
                
                if (testDefinition != null)
                {
                    testDefinitions.Add(testDefinition);
                }
                
                testIndex++;
            }
        }
        
        return testDefinitions;
    }
    
    private CompiledFactories CompileFactories(TestMetadata metadata)
    {
        return new CompiledFactories
        {
            ClassFactory = GetOrCompileConstructor(metadata.TestClassType),
            MethodInvoker = GetOrCompileMethodInvoker(metadata.TestMethod),
            PropertySetters = metadata.PropertyDataSources.Keys
                .ToDictionary(p => p, GetOrCompilePropertySetter)
        };
    }
    
    private Func<object?[], object> GetOrCompileConstructor(Type type)
    {
        return ConstructorInvokerCache.GetOrAdd(type, t =>
        {
            var ctor = ConstructorCache.GetOrAdd(t, type =>
                type.GetConstructors()
                    .OrderBy(c => c.GetParameters().Length)
                    .FirstOrDefault());
            
            if (ctor == null)
            {
                return args => throw new InvalidOperationException($"No accessible constructor found for {type.Name}");
            }
            
            // Compile expression for faster invocation
            var parameters = Expression.Parameter(typeof(object?[]), "args");
            var ctorParams = ctor.GetParameters();
            
            var arguments = new Expression[ctorParams.Length];
            for (int i = 0; i < ctorParams.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = ctorParams[i].ParameterType;
                var argument = Expression.ArrayIndex(parameters, index);
                arguments[i] = Expression.Convert(argument, paramType);
            }
            
            var newExpr = Expression.New(ctor, arguments);
            var lambda = Expression.Lambda<Func<object?[], object>>(
                Expression.Convert(newExpr, typeof(object)),
                parameters);
            
            return lambda.Compile();
        });
    }
    
    private Func<object?, object?[], object?> GetOrCompileMethodInvoker(MethodInfo method)
    {
        return MethodInvokerCache.GetOrAdd(method, m =>
        {
            // Compile expression for method invocation
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var argsParam = Expression.Parameter(typeof(object?[]), "args");
            
            var methodParams = m.GetParameters();
            var arguments = new Expression[methodParams.Length];
            
            for (int i = 0; i < methodParams.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = methodParams[i].ParameterType;
                var argument = Expression.ArrayIndex(argsParam, index);
                arguments[i] = Expression.Convert(argument, paramType);
            }
            
            var instanceCast = m.IsStatic ? null : Expression.Convert(instanceParam, m.DeclaringType!);
            var methodCall = m.IsStatic 
                ? Expression.Call(m, arguments)
                : Expression.Call(instanceCast!, m, arguments);
            
            var lambda = Expression.Lambda<Func<object?, object?[], object?>>(
                Expression.Convert(methodCall, typeof(object)),
                instanceParam,
                argsParam);
            
            return lambda.Compile();
        });
    }
    
    private Action<object, object?> GetOrCompilePropertySetter(PropertyInfo property)
    {
        return PropertySetterCache.GetOrAdd(property, p =>
        {
            if (!p.CanWrite)
            {
                return (obj, value) => throw new InvalidOperationException($"Property {p.Name} is read-only");
            }
            
            // Compile expression for property setter
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var valueParam = Expression.Parameter(typeof(object), "value");
            
            var instanceCast = Expression.Convert(instanceParam, p.DeclaringType!);
            var valueCast = Expression.Convert(valueParam, p.PropertyType);
            
            var propertyAccess = Expression.Property(instanceCast, p);
            var assignment = Expression.Assign(propertyAccess, valueCast);
            
            var lambda = Expression.Lambda<Action<object, object?>>(
                assignment,
                instanceParam,
                valueParam);
            
            return lambda.Compile();
        });
    }
    
    private async Task<List<TestCombination>> GetTestCombinationsAsync(
        TestMetadata metadata, 
        CancellationToken cancellationToken)
    {
        var combinations = new List<TestCombination>();
        
        // Get all data sets in parallel where possible
        var classDataTask = GetDataSetsAsync(metadata.ClassDataSources, cancellationToken);
        var methodDataTask = GetDataSetsAsync(metadata.MethodDataSources, cancellationToken);
        
        var classDataSets = await classDataTask;
        var methodDataSets = await methodDataTask;
        
        // Get property data sets
        var propertyDataTasks = metadata.PropertyDataSources
            .Select(async kvp => (kvp.Key, await GetDataSetsAsync(new[] { kvp.Value }, cancellationToken)))
            .ToArray();
        
        var propertyDataResults = await Task.WhenAll(propertyDataTasks);
        var propertyDataSets = propertyDataResults.ToDictionary(r => r.Key, r => r.Item2);
        
        // Generate all combinations
        if (!classDataSets.Any())
            classDataSets.Add(Array.Empty<object?>());
        if (!methodDataSets.Any())
            methodDataSets.Add(Array.Empty<object?>());
        
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
        
        return combinations;
    }
    
    private async Task<List<object?[]>> GetDataSetsAsync(
        IEnumerable<IDataSourceProvider> dataSourceProviders, 
        CancellationToken cancellationToken)
    {
        var allDataSets = new List<object?[]>();
        
        // Group by async vs sync to optimize execution
        var providers = dataSourceProviders.ToList();
        var asyncProviders = providers.Where(p => p.IsAsync).ToList();
        var syncProviders = providers.Where(p => !p.IsAsync).ToList();
        
        // Process sync providers first (fast)
        foreach (var provider in syncProviders)
        {
            foreach (var data in provider.GetData())
            {
                allDataSets.Add(data);
            }
        }
        
        // Process async providers in parallel where possible
        if (asyncProviders.Any())
        {
            var asyncTasks = asyncProviders.Select(async provider =>
            {
                var results = new List<object?[]>();
                await foreach (var data in provider.GetDataAsync().WithCancellation(cancellationToken))
                {
                    results.Add(data);
                }
                return results;
            });
            
            var asyncResults = await Task.WhenAll(asyncTasks);
            foreach (var results in asyncResults)
            {
                allDataSets.AddRange(results);
            }
        }
        
        return allDataSets;
    }
    
    private async Task<TestDefinition?> BuildSingleTestDefinitionAsync(
        TestMetadata metadata,
        TestCombination combination,
        int testIndex,
        int repeatIndex,
        CompiledFactories factories,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build test ID from template
            var testId = BuildTestId(metadata.TestIdTemplate, testIndex, repeatIndex);
            
            // Unwrap tuples if necessary
            var unwrappedClassArgs = UnwrapTuplesOptimized(combination.ClassArguments);
            var unwrappedMethodArgs = UnwrapTuplesOptimized(combination.MethodArguments);
            
            // Create factories that capture the current combination
            var classFactory = CreateOptimizedClassFactory(metadata, unwrappedClassArgs, combination.PropertyValues, factories);
            var methodInvoker = CreateOptimizedMethodInvoker(metadata, unwrappedMethodArgs, factories);
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
            // Log error and skip this test combination
            Console.WriteLine($"Failed to build test definition: {ex.Message}");
            return null;
        }
    }
    
    private string BuildTestId(string template, int testIndex, int repeatIndex)
    {
        // Use StringBuilder for better performance
        var result = new System.Text.StringBuilder(template);
        result.Replace("{TestIndex}", testIndex.ToString());
        result.Replace("{RepeatIndex}", repeatIndex.ToString());
        return result.ToString();
    }
    
    private object?[] UnwrapTuplesOptimized(object?[] arguments)
    {
        // Fast path for non-tuple arguments
        if (arguments.Length != 1 || arguments[0] == null)
            return arguments;
        
        var argType = arguments[0].GetType();
        if (!IsTupleTypeCached(argType))
            return arguments;
        
        return UnwrapTupleOptimized(arguments[0]);
    }
    
    private bool IsTupleTypeCached(Type type)
    {
        return TupleTypeCache.GetOrAdd(type, t =>
            t.IsGenericType && t.FullName?.StartsWith("System.ValueTuple`") == true);
    }
    
    private object?[] UnwrapTupleOptimized(object tuple)
    {
        var tupleType = tuple.GetType();
        var values = new List<object?>();
        
        // Use cached field info for better performance
        var fields = tupleType.GetFields()
            .Where(f => f.Name.StartsWith("Item") || f.Name == "Rest")
            .OrderBy(f => f.Name)
            .ToArray();
        
        foreach (var field in fields)
        {
            var value = field.GetValue(tuple);
            
            if (field.Name == "Rest" && value != null && IsTupleTypeCached(value.GetType()))
            {
                values.AddRange(UnwrapTupleOptimized(value));
            }
            else if (field.Name != "Rest")
            {
                values.Add(value);
            }
        }
        
        return values.ToArray();
    }
    
    private Func<object> CreateOptimizedClassFactory(
        TestMetadata metadata,
        object?[] classArgs,
        Dictionary<PropertyInfo, object?> propertyValues,
        CompiledFactories factories)
    {
        return () =>
        {
            var instance = factories.ClassFactory(classArgs);
            
            // Set properties using compiled setters
            foreach (var (property, value) in propertyValues)
            {
                try
                {
                    // Handle async initialization if needed
                    if (value is IAsyncInitializer asyncInitializer)
                    {
                        // Note: This is sync-over-async which isn't ideal
                        Task.Run(async () => await ObjectInitializer.InitializeAsync(asyncInitializer)).GetAwaiter().GetResult();
                    }
                    
                    factories.PropertySetters[property](instance, value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to set property {property.Name} on {metadata.TestClassType.Name}", ex);
                }
            }
            
            return instance;
        };
    }
    
    private Func<object, CancellationToken, ValueTask> CreateOptimizedMethodInvoker(
        TestMetadata metadata, 
        object?[] methodArgs,
        CompiledFactories factories)
    {
        return async (instance, cancellationToken) =>
        {
            var result = factories.MethodInvoker(instance, methodArgs);
            
            if (result is Task task)
            {
                await task;
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask;
            }
            // For sync methods, nothing to await
        };
    }
    
    private Func<IDictionary<string, object?>> CreatePropertiesProvider(
        Dictionary<PropertyInfo, object?> propertyValues)
    {
        // Pre-build the dictionary to avoid repeated creation
        var properties = propertyValues.ToDictionary(
            kvp => kvp.Key.Name,
            kvp => kvp.Value);
        
        return () => properties;
    }
    
    private class TestCombination
    {
        public required object?[] ClassArguments { get; init; }
        public required object?[] MethodArguments { get; init; }
        public required Dictionary<PropertyInfo, object?> PropertyValues { get; init; }
    }
    
    private class CompiledFactories
    {
        public required Func<object?[], object> ClassFactory { get; init; }
        public required Func<object?, object?[], object?> MethodInvoker { get; init; }
        public required Dictionary<PropertyInfo, Action<object, object?>> PropertySetters { get; init; }
    }
}