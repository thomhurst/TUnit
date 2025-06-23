using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// Builds TestDefinition instances from DynamicTestMetadata at runtime.
/// This class contains all the complex logic for expanding data sources, unwrapping tuples,
/// and creating test instances using reflection and expression compilation.
/// </summary>
[RequiresDynamicCode("DynamicTestBuilder uses dynamic code generation for performance optimization and generic type support")]
[RequiresUnreferencedCode("DynamicTestBuilder may use types that aren't statically referenced when resolving generic types")]
public class DynamicTestBuilder : ITestDefinitionBuilder
{
    // Caches for reflection operations - thread-safe for concurrent test discovery
    private static readonly ConcurrentDictionary<Type, ConstructorInfo?> ConstructorCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyCache = new();
    private static readonly ConcurrentDictionary<Type, bool> TupleTypeCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, Func<object?, object?[], object?>> MethodInvokerCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> PropertySetterCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object?[], object>> ConstructorInvokerCache = new();

    /// <summary>
    /// Builds all test definitions from the given test descriptor.
    /// </summary>
    public async Task<IEnumerable<TestDefinition>> BuildTestDefinitionsAsync(ITestDescriptor testDescriptor, CancellationToken cancellationToken = default)
    {
        if (testDescriptor is not DynamicTestMetadata metadata)
        {
            throw new ArgumentException($"DynamicTestBuilder can only process DynamicTestMetadata, not {testDescriptor.GetType().Name}");
        }
        
        return await BuildTestsFromDynamicMetadataAsync(metadata, cancellationToken);
    }
    
    /// <summary>
    /// Builds all test definitions from the given dynamic metadata.
    /// Handles all data source combinations, tuple unwrapping, and property initialization.
    /// </summary>
    private async Task<IEnumerable<TestDefinition>> BuildTestsFromDynamicMetadataAsync(DynamicTestMetadata metadata, CancellationToken cancellationToken = default)
    {

        var testDefinitions = new List<TestDefinition>();

        // Pre-compile delegates for this metadata for better performance
        var compiledFactories = CompileFactories(metadata);

        // Get all combinations of class and method data
        var testCombinations = await GetTestCombinationsAsync(metadata, cancellationToken);

        // Test combinations ready

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

        // Test building complete

        return testDefinitions;
    }

    private CompiledFactories CompileFactories(DynamicTestMetadata metadata)
    {
        // For generic types, we need to resolve the type reference first
        var testClassType = metadata.TestClassType;
        if (testClassType == null)
        {
            // This is a generic type - we'll need to resolve it later per test instance
            // For now, create a placeholder that will be replaced
            return new CompiledFactories
            {
                ClassFactory = null!, // Will be set per test instance
                MethodInvoker = null!, // Will be set per test instance
                PropertySetters = new Dictionary<PropertyInfo, Action<object, object?>>()
            };
        }
        
        // Check if the method is a generic method definition or if it's defined on a generic type
        var methodInfo = metadata.MethodMetadata.ReflectionInformation;
        var declaringType = methodInfo.DeclaringType;
        
        // If the declaring type is generic and different from our test class type,
        // we're dealing with an inherited generic method
        if (declaringType != null && declaringType.IsGenericTypeDefinition && declaringType != testClassType)
        {
            // We can't compile this at this stage - defer to runtime
            return new CompiledFactories
            {
                ClassFactory = GetOrCompileConstructor(testClassType),
                MethodInvoker = null!, // Will be resolved at runtime
                PropertySetters = metadata.PropertyDataSources.Keys
                    .ToDictionary(p => p, GetOrCompilePropertySetter)
            };
        }
        
        Func<object?, object?[], object?> methodInvoker;
        
        if (methodInfo.IsGenericMethodDefinition)
        {
            // For generic methods, we can't pre-compile the invoker
            // We'll create a dynamic invoker that handles type inference at runtime
            methodInvoker = CreateGenericMethodInvoker(methodInfo);
        }
        else
        {
            methodInvoker = GetOrCompileMethodInvoker(methodInfo);
        }
        
        return new CompiledFactories
        {
            ClassFactory = GetOrCompileConstructor(testClassType),
            MethodInvoker = methodInvoker,
            PropertySetters = metadata.PropertyDataSources.Keys
                .ToDictionary(p => p, GetOrCompilePropertySetter)
        };
    }

    private Func<object?[], object> GetOrCompileConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        return ConstructorInvokerCache.GetOrAdd(type, t =>
        {
            #pragma warning disable IL2070 // Type parameter is annotated for constructors
            var ctor = ConstructorCache.GetOrAdd(t, type =>
                type.GetConstructors()
                    .OrderBy(c => c.GetParameters().Length)
                    .FirstOrDefault());
            #pragma warning restore IL2070

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

    private Func<object?, object?[], object?> CreateGenericMethodInvoker(MethodInfo genericMethodDefinition)
    {
        return (instance, args) =>
        {
            // Infer type arguments from the actual argument types
            var typeArgs = new Type[genericMethodDefinition.GetGenericArguments().Length];
            var methodParams = genericMethodDefinition.GetParameters();
            
            // Simple type inference based on argument types
            for (int i = 0; i < methodParams.Length && i < args.Length; i++)
            {
                if (args[i] != null)
                {
                    var paramType = methodParams[i].ParameterType;
                    if (paramType.IsGenericParameter)
                    {
                        var genericParamPosition = Array.IndexOf(genericMethodDefinition.GetGenericArguments(), paramType);
                        if (genericParamPosition >= 0)
                        {
                            typeArgs[genericParamPosition] = args[i]!.GetType();
                        }
                    }
                }
            }
            
            // Fill in any remaining type arguments with object
            for (int i = 0; i < typeArgs.Length; i++)
            {
                typeArgs[i] ??= typeof(object);
            }
            
            // Make the generic method concrete
            var concreteMethod = genericMethodDefinition.MakeGenericMethod(typeArgs);
            
            // Now we can compile and cache the concrete method
            var invoker = GetOrCompileMethodInvoker(concreteMethod);
            return invoker(instance, args);
        };
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

            // Handle void methods
            Expression body;
            if (m.ReturnType == typeof(void))
            {
                body = Expression.Block(methodCall, Expression.Constant(null, typeof(object)));
            }
            else
            {
                body = Expression.Convert(methodCall, typeof(object));
            }

            var lambda = Expression.Lambda<Func<object?, object?[], object?>>(
                body,
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
        DynamicTestMetadata metadata,
        CancellationToken cancellationToken)
    {
        var combinations = new List<TestCombination>();

        // Get all data sets in parallel for better performance
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
        {
            classDataSets.Add(Array.Empty<object?>());
        }
        if (!methodDataSets.Any())
        {
            methodDataSets.Add(Array.Empty<object?>());
        }

        foreach (var classData in classDataSets)
        {
            foreach (var methodData in methodDataSets)
            {
                var propertyData = new Dictionary<PropertyInfo, object?>();

                // Get property values for this combination
                foreach (var (property, dataSets) in propertyDataSets)
                {
                    if (dataSets.Any())
                    {
                        // For now, take the first value - could be enhanced to create more combinations
                        propertyData[property] = dataSets.First().FirstOrDefault();
                    }
                }

                combinations.Add(new TestCombination
                {
                    ClassArguments = classData,
                    MethodArguments = methodData,
                    PropertyValues = propertyData
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

        // Process data sources in parallel for better performance
        var tasks = dataSourceProviders.Select(async provider =>
        {
            var data = provider.GetDataAsync();
            var dataList = new List<object?[]>();
            await foreach (var item in data.WithCancellation(cancellationToken))
            {
                dataList.Add(item);
            }
            return dataList;
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var dataSet in results.SelectMany(r => r))
        {
            allDataSets.Add(dataSet);
        }

        return allDataSets;
    }

    private async Task<TestDefinition?> BuildSingleTestDefinitionAsync(
        DynamicTestMetadata metadata,
        TestCombination combination,
        int testIndex,
        int repeatIndex,
        CompiledFactories factories,
        CancellationToken cancellationToken)
    {
        // Create unique test ID
        var testId = metadata.TestIdTemplate
            .Replace("{TestIndex}", testIndex.ToString())
            .Replace("{RepeatIndex}", repeatIndex.ToString());

        // Build display name
        var displayName = BuildDisplayName(metadata.DisplayNameTemplate, combination.MethodArguments);

        // Create test class factory that also sets properties
        Func<object> testClassFactory = () =>
        {
            object instance;
            
            // Handle generic types - resolve TypeReference at runtime
            if (metadata.TestClassType == null && metadata.TestClassTypeReference != null)
            {
                // For generic types, we need to resolve the type reference
                // This typically happens when we have concrete derived classes
                #pragma warning disable IL3050, IL2026, IL2072 // Generic type resolution requires dynamic code
                var resolver = TypeResolver.CreateSimple();
                var resolvedType = resolver.Resolve(metadata.TestClassTypeReference);
                var ctor = GetOrCompileConstructor(resolvedType);
                instance = ctor(combination.ClassArguments);
                #pragma warning restore IL3050, IL2026, IL2072
                
                // Apply property values
                if (instance != null && combination.PropertyValues.Any())
                {
                    foreach (var (property, value) in combination.PropertyValues)
                    {
                        property.SetValue(instance, value);
                    }
                }
            }
            else
            {
                // Non-generic case - use pre-compiled factory
                instance = factories.ClassFactory(combination.ClassArguments);

                // Apply property values
                if (instance != null && factories.PropertySetters.Any())
                {
                    foreach (var (property, setter) in factories.PropertySetters)
                    {
                        if (combination.PropertyValues.TryGetValue(property, out var value))
                        {
                            setter(instance, value);
                        }
                    }
                }
            }

            return instance!;
        };

        // Create test method invoker
        Func<object, CancellationToken, ValueTask> testMethodInvoker = async (instance, cancellationToken) =>
        {
            object? result;
            
            // If we're dealing with a generic type that was resolved at runtime, 
            // or if the method invoker is null (deferred compilation)
            if ((metadata.TestClassType == null || factories.MethodInvoker == null) && instance != null)
            {
                var concreteType = instance.GetType();
                var method = metadata.MethodMetadata.ReflectionInformation;
                
                // Find the corresponding method on the concrete type
                var concreteMethod = concreteType.GetMethod(
                    method.Name, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                
                if (concreteMethod != null && concreteMethod.IsGenericMethodDefinition)
                {
                    // Use our generic method invoker
                    var genericInvoker = CreateGenericMethodInvoker(concreteMethod);
                    result = genericInvoker(instance, combination.MethodArguments);
                }
                else if (concreteMethod != null)
                {
                    // Get or compile invoker for the concrete method
                    var invoker = GetOrCompileMethodInvoker(concreteMethod);
                    result = invoker(instance, combination.MethodArguments);
                }
                else
                {
                    throw new InvalidOperationException($"Could not find method {method.Name} on type {concreteType}");
                }
            }
            else
            {
                result = factories.MethodInvoker?.Invoke(instance, combination.MethodArguments);
            }

            // Handle async methods
            if (result is Task task)
            {
                await task;
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask;
            }
        };

        // Create property setter that also applies the values
        Func<IDictionary<string, object?>> propertiesProvider = () =>
        {
            var props = new Dictionary<string, object?>();
            foreach (var (property, value) in combination.PropertyValues)
            {
                props[property.Name] = value;
            }
            return props;
        };


        // Handle tuple unwrapping for method arguments
        var unwrappedMethodArgs = await UnwrapTuplesAsync(combination.MethodArguments, cancellationToken);

        return new TestDefinition
        {
            TestId = testId,
            MethodMetadata = metadata.MethodMetadata,
            TestFilePath = metadata.TestFilePath,
            TestLineNumber = metadata.TestLineNumber,
            TestClassFactory = testClassFactory,
            TestMethodInvoker = testMethodInvoker,
            PropertiesProvider = propertiesProvider
        };
    }

    private string BuildDisplayName(string template, object?[] methodArguments)
    {
        var displayName = template;

        // Replace placeholders with actual values
        for (int i = 0; i < methodArguments.Length; i++)
        {
            var value = methodArguments[i];
            var formattedValue = FormatArgumentValue(value);
            displayName = displayName.Replace($"{{{i}}}", formattedValue);
        }

        return displayName;
    }

    private string FormatArgumentValue(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string str)
        {
            return $"\"{str}\"";
        }

        if (value is char ch)
        {
            return $"'{ch}'";
        }

        if (value is bool b)
        {
            return b.ToString().ToLower();
        }

        if (value.GetType().IsArray)
        {
            var array = (Array)value;
            var elements = new List<string>();
            foreach (var element in array)
            {
                elements.Add(FormatArgumentValue(element));
            }
            return $"[{string.Join(", ", elements)}]";
        }

        return value.ToString() ?? "null";
    }

    private Task<object?[]> UnwrapTuplesAsync(object?[] arguments, CancellationToken cancellationToken)
    {
        if (arguments.Length != 1)
        {
            return Task.FromResult(arguments);
        }

        var singleArg = arguments[0];
        if (singleArg == null)
        {
            return Task.FromResult(arguments);
        }

        var argType = singleArg.GetType();
        if (!IsTupleType(argType))
        {
            return Task.FromResult(arguments);
        }

        // Unwrap tuple into individual values
        var tupleValues = new List<object?>();

        #pragma warning disable IL2075 // We know tuples have public fields
        var fields = argType.GetFields();
        #pragma warning restore IL2075

        foreach (var field in fields.Where(f => f.Name.StartsWith("Item")))
        {
            var value = field.GetValue(singleArg);
            tupleValues.Add(value);
        }

        return Task.FromResult(tupleValues.ToArray());
    }

    private bool IsTupleType(Type type)
    {
        return TupleTypeCache.GetOrAdd(type, t =>
        {
            if (!t.IsGenericType)
            {
                return false;
            }

            var genericTypeDef = t.GetGenericTypeDefinition();
            return genericTypeDef == typeof(ValueTuple<>) ||
                   genericTypeDef == typeof(ValueTuple<,>) ||
                   genericTypeDef == typeof(ValueTuple<,,>) ||
                   genericTypeDef == typeof(ValueTuple<,,,>) ||
                   genericTypeDef == typeof(ValueTuple<,,,,>) ||
                   genericTypeDef == typeof(ValueTuple<,,,,,>) ||
                   genericTypeDef == typeof(ValueTuple<,,,,,,>) ||
                   genericTypeDef == typeof(ValueTuple<,,,,,,,>) ||
                   genericTypeDef == typeof(Tuple<>) ||
                   genericTypeDef == typeof(Tuple<,>) ||
                   genericTypeDef == typeof(Tuple<,,>) ||
                   genericTypeDef == typeof(Tuple<,,,>) ||
                   genericTypeDef == typeof(Tuple<,,,,>) ||
                   genericTypeDef == typeof(Tuple<,,,,,>) ||
                   genericTypeDef == typeof(Tuple<,,,,,,>) ||
                   genericTypeDef == typeof(Tuple<,,,,,,,>);
        });
    }

    private class TestCombination
    {
        public object?[] ClassArguments { get; set; } = Array.Empty<object?>();
        public object?[] MethodArguments { get; set; } = Array.Empty<object?>();
        public Dictionary<PropertyInfo, object?> PropertyValues { get; set; } = new();
    }

    private class CompiledFactories
    {
        public Func<object?[], object> ClassFactory { get; set; } = null!;
        public Func<object?, object?[], object?> MethodInvoker { get; set; } = null!;
        public Dictionary<PropertyInfo, Action<object, object?>> PropertySetters { get; set; } = new();
    }
}


