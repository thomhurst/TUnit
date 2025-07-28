using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Test metadata implementation that uses reflection for legacy/discovery scenarios
/// </summary>
internal sealed class ReflectionTestMetadata : TestMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    private readonly Type _testClass;
    private readonly MethodInfo _testMethod;

    public ReflectionTestMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods
            | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass,
        MethodInfo testMethod)
    {
        _testClass = testClass;
        _testMethod = testMethod;
    }

    [field: AllowNull, MaybeNull]
    public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            if (field == null)
            {
                field = CreateExecutableTest;
            }
            return field;
        }
    }

    private async IAsyncEnumerable<TestDataCombination> GenerateDataCombinations(TestBuilderContextAccessor? contextAccessor)
    {
        // Wrap the entire data generation in error handling
        await foreach (var combination in DataCombinationBuilder.BuildCombinationsWithErrorHandlingAsync(GenerateDataCombinationsCore))
        {
            yield return combination;
        }
    }

    #pragma warning disable CS1998 // Async method lacks 'await' operators
    private async IAsyncEnumerable<TestDataCombination> GenerateDataCombinationsCore()
    #pragma warning restore CS1998
    {
        // Extract data sources from attributes using reflection
        var methodDataSources = ExtractMethodDataSources();
        var classDataSources = ExtractClassDataSources();
        var propertyDataSources = ExtractPropertyDataSources();

        // Get repeat count from method, class, or assembly level
        var repeatCount = GetRepeatCount();

        // If no data sources and no repeat, yield a single empty combination
        if (!methodDataSources.Any() && !classDataSources.Any() && !propertyDataSources.Any())
        {
            for (var repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
            {
                yield return new TestDataCombination
                {
                    MethodDataFactories = [
                    ],
                    ClassDataFactories = [
                    ],
                    MethodDataSourceIndex = -1,
                    MethodLoopIndex = 0,
                    ClassDataSourceIndex = -1,
                    ClassLoopIndex = 0,
                    RepeatIndex = repeatIndex
                };
            }
            yield break;
        }

        // Generate all combinations without awaiting inside the async enumerable
        var methodDataCombinations = await ProcessDataSourcesAsync(methodDataSources, ProcessMethodDataSourceAsync);
        var classDataCombinations = await ProcessDataSourcesAsync(classDataSources, ProcessClassDataSourceAsync);
        var propertyDataCombinations = await GeneratePropertyDataCombinationsAsync(propertyDataSources);

        // Use the unified DataCombinationBuilder
        await foreach (var combination in DataCombinationBuilder.BuildCombinationsAsync(
            methodDataCombinations,
            classDataCombinations,
            propertyDataCombinations,
            repeatCount))
        {
            yield return combination;
        }
    }


    private async Task<List<T>> ProcessDataSourcesAsync<T>(List<IDataSourceAttribute> sources, Func<IDataSourceAttribute, Task<IEnumerable<T>>> processor)
        where T : new()
    {
        var results = new List<T>();

        foreach (var source in sources)
        {
            try
            {
                var items = await processor(source);
                results.AddRange(items);
            }
            catch (Exception ex)
            {
                // Instead of logging and continuing, create an error combination
                // This will be caught by the outer error handler and converted to TestDataCombination with DataGenerationException
                throw new Exception($"Failed to process data source: {ex.Message}", ex);
            }
        }

        return results;
    }

    private ExecutableTest CreateExecutableTest(ExecutableTestCreationContext context, TestMetadata metadata)
    {
        // Create instance factory that uses reflection
        #pragma warning disable CS1998 // Async method lacks 'await' operators
        Func<TestContext, Task<object>> createInstance = async (testContext) =>
        {
            #pragma warning restore CS1998
            if (InstanceFactory == null)
            {
                throw new InvalidOperationException($"No instance factory for {_testClass.Name}");
            }

            // Get type arguments for generic types
            // For generic types, we need to infer the type arguments from the actual argument values
            Type[] typeArgs;
            if (_testClass.IsGenericTypeDefinition && context.ClassArguments != null && context.ClassArguments.Length > 0)
            {
                // Infer type arguments from the constructor argument values
                var genericParams = _testClass.GetGenericArguments();
                typeArgs = new Type[genericParams.Length];
                
                // For single generic parameter, use the first argument's type
                if (genericParams.Length == 1 && context.ClassArguments.Length >= 1)
                {
                    typeArgs[0] = context.ClassArguments[0]?.GetType() ?? typeof(object);
                }
                else
                {
                    // For multiple generic parameters, try to match one-to-one
                    for (var i = 0; i < genericParams.Length; i++)
                    {
                        if (i < context.ClassArguments.Length && context.ClassArguments[i] != null)
                        {
                            typeArgs[i] = context.ClassArguments[i]!.GetType();
                        }
                        else
                        {
                            typeArgs[i] = typeof(object);
                        }
                    }
                }
            }
            else
            {
                typeArgs = testContext.TestDetails.TestClassArguments?.OfType<Type>().ToArray() ?? Type.EmptyTypes;
            }
            
            var instance = InstanceFactory(typeArgs, context.ClassArguments ?? Array.Empty<object?>());

            // Apply property values using unified PropertyInjector
            await PropertyInjector.InjectPropertiesAsync(
                context.Context,
                instance,
                metadata.PropertyDataSources,
                metadata.PropertyInjections,
                metadata.MethodMetadata,
                context.Context.TestDetails.TestId);

            return instance;
        };

        // Create test invoker with CancellationToken support
        // Determine if the test method has a CancellationToken parameter
        var hasCancellationToken = ParameterTypes.Any(t => t == typeof(CancellationToken));
        var cancellationTokenIndex = hasCancellationToken
            ? Array.IndexOf(ParameterTypes, typeof(CancellationToken))
            : -1;

        Func<object, object?[], TestContext, CancellationToken, Task> invokeTest = async (instance, args, testContext, cancellationToken) =>
        {
            if (TestInvoker == null)
            {
                throw new InvalidOperationException($"No test invoker for {_testMethod.Name}");
            }

            if (hasCancellationToken)
            {
                // Insert CancellationToken at the correct position
                var argsWithToken = new object?[args.Length + 1];
                var argIndex = 0;

                for (var i = 0; i < argsWithToken.Length; i++)
                {
                    if (i == cancellationTokenIndex)
                    {
                        argsWithToken[i] = cancellationToken;
                    }
                    else if (argIndex < args.Length)
                    {
                        argsWithToken[i] = args[argIndex++];
                    }
                }

                await TestInvoker(instance, argsWithToken);
            }
            else
            {
                await TestInvoker(instance, args);
            }
        };

        return new UnifiedExecutableTest(createInstance, invokeTest)
        {
            TestId = context.TestId,
            DisplayName = context.DisplayName,
            Metadata = metadata,
            Arguments = context.Arguments,
            ClassArguments = context.ClassArguments,
            Context = context.Context
        };
    }

    private List<IDataSourceAttribute> ExtractMethodDataSources()
    {
        // Use the data sources already extracted by ReflectionTestDataCollector
        // This includes custom data source generators like MatrixDataSourceAttribute
        if (DataSources is { Length: > 0 })
        {
            return DataSources.ToList();
        }

        // Fallback to extracting from attributes if not already set
        var attributes = _testMethod.GetCustomAttributes().ToList();
        var dataSources = attributes.OfType<IDataSourceAttribute>().ToList();

        if (dataSources.Count == 0)
        {
            return [new EmptyDataSourceAttribute()];
        }

        return dataSources;
    }

    private List<IDataSourceAttribute> ExtractClassDataSources()
    {
        // Use the data sources already extracted by ReflectionTestDataCollector
        if (ClassDataSources is { Length: > 0 })
        {
            return ClassDataSources.ToList();
        }

        // Fallback to extracting from attributes if not already set
        var attributes = _testClass.GetCustomAttributes().ToList();
        return attributes.OfType<IDataSourceAttribute>().ToList();
    }

    private List<PropertyDataSource> ExtractPropertyDataSources()
    {
        // Use the property data sources already extracted by ReflectionTestDataCollector
        if (PropertyDataSources is { Length: > 0 })
        {
            return PropertyDataSources.ToList();
        }

        // Fallback to extracting from attributes if not already set
        var sources = new List<PropertyDataSource>();

        var properties = _testClass.GetProperties()
            .Where(p => p.GetCustomAttributes().Any(a => a is IDataSourceAttribute))
            .ToList();

        foreach (var property in properties)
        {
            var attributes = property.GetCustomAttributes().ToList();

            // Process all IDataSourceAttribute attributes on properties
            foreach (var attr in attributes.OfType<IDataSourceAttribute>())
            {
                try
                {
                    // Special handling for ArgumentsAttribute which needs to be wrapped
                    if (attr is ArgumentsAttribute argsAttr)
                    {
                        sources.Add(new PropertyDataSource
                        {
                            PropertyName = property.Name,
                            PropertyType = property.PropertyType,
                            DataSource = new StaticDataSourceAttribute(argsAttr.Values)
                        });
                    }
                    else
                    {
                        // All other data source attributes can be used directly
                        sources.Add(new PropertyDataSource
                        {
                            PropertyName = property.Name,
                            PropertyType = property.PropertyType,
                            DataSource = attr
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Property data source failure will be wrapped in TestDataCombination by error handling
                    throw new Exception($"Failed to create property data source for {property.Name}: {ex.Message}", ex);
                }
            }
        }

        return sources;
    }


    private int GetRepeatCount()
    {
        // Check method level first
        var methodRepeat = _testMethod.GetCustomAttribute<RepeatAttribute>();
        if (methodRepeat != null)
        {
            // Times represents additional repeats, so total executions = Times + 1
            return methodRepeat.Times + 1;
        }

        // Check class level
        var classRepeat = _testClass.GetCustomAttribute<RepeatAttribute>();
        if (classRepeat != null)
        {
            // Times represents additional repeats, so total executions = Times + 1
            return classRepeat.Times + 1;
        }

        // Check assembly level
        var assemblyRepeat = _testClass.Assembly.GetCustomAttribute<RepeatAttribute>();
        if (assemblyRepeat != null)
        {
            // Times represents additional repeats, so total executions = Times + 1
            return assemblyRepeat.Times + 1;
        }

        return 1; // Default to 1 if no repeat attribute found
    }


    private async Task<IEnumerable<MethodDataCombination>> ProcessMethodDataSourceAsync(IDataSourceAttribute dataSource)
    {
        var combinations = new List<MethodDataCombination>();
        var loopIndex = 0;

        var metadata = CreateDataGeneratorMetadata(global::TUnit.Core.Enums.DataGeneratorType.TestParameters);

        await foreach (var rowFactory in dataSource.GetDataRowsAsync(metadata))
        {
            var row = await rowFactory();
            if (row == null)
            {
                continue;
            }

            var dataFactories = row.Select(value => new Func<Task<object?>>(async () =>
            {
                var resolvedValue = await ResolveTestDataValueAsync(value);
                return resolvedValue;
            })).ToArray();

            combinations.Add(new MethodDataCombination
            {
                DataFactories = dataFactories,
                DataSourceIndex = 0,
                LoopIndex = loopIndex++
            });
        }

        return combinations;
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Reflection mode cannot support AOT")]
    private static async Task<object?> ResolveTestDataValueAsync(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Check if it's a Func<Task<T>>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            var returnType = type.GetGenericArguments()[0];

            // Invoke the Func to get the result
            var invokeMethod = type.GetMethod("Invoke");
            var result = invokeMethod!.Invoke(value, null);

            // If the result is a Task, await it
            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                // Get the Result property for Task<T>
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }

                // For non-generic Task
                return null;
            }

            return result;
        }

        // Check if it's already a Task<T>
        if (value is Task task2)
        {
            await task2.ConfigureAwait(false);

            var taskType = task2.GetType();
            if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task2);
            }

            return null;
        }

        // Check for other delegate types that might need invocation
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var invokeMethod = type.GetMethod("Invoke");
            if (invokeMethod != null && invokeMethod.GetParameters().Length == 0)
            {
                // It's a parameterless delegate, invoke it
                var result = invokeMethod.Invoke(value, null);

                // Recursively resolve in case it returns a Task
                return await ResolveTestDataValueAsync(result).ConfigureAwait(false);
            }
        }

        return value;
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Reflection mode cannot support AOT")]
    private static object? ResolveTestDataValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Check if it's a Func<T> (has Invoke method with no parameters and returns something)
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Func<>))
        {
            // Invoke the Func<T> to get the actual value
            return type.GetMethod("Invoke")!.Invoke(value, null);
        }

        // Check for other delegate types that might need invocation
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var invokeMethod = type.GetMethod("Invoke");
            if (invokeMethod != null && invokeMethod.GetParameters().Length == 0)
            {
                // It's a parameterless delegate, invoke it
                return invokeMethod.Invoke(value, null);
            }
        }

        return value;
    }

    // Method removed - replaced by single async implementation above

    private async Task<IEnumerable<ClassDataCombination>> ProcessClassDataSourceAsync(IDataSourceAttribute dataSource)
    {
        var combinations = new List<ClassDataCombination>();
        var loopIndex = 0;

        var metadata = CreateDataGeneratorMetadata(global::TUnit.Core.Enums.DataGeneratorType.ClassParameters);

        await foreach (var rowFactory in dataSource.GetDataRowsAsync(metadata))
        {
            var row = await rowFactory();
            if (row == null)
            {
                continue;
            }

            var dataFactories = row.Select(value => new Func<Task<object?>>(async () =>
            {
                var resolvedValue = await ResolveTestDataValueAsync(value);
                return resolvedValue;
            })).ToArray();

            combinations.Add(new ClassDataCombination
            {
                DataFactories = dataFactories,
                DataSourceIndex = 0,
                LoopIndex = loopIndex++
            });
        }

        return combinations;
    }

    // Method removed - replaced by single async implementation above

    private async Task<List<PropertyDataCombination>> GeneratePropertyDataCombinationsAsync(List<PropertyDataSource> propertyDataSources)
    {
        var combinations = new List<PropertyDataCombination>();

        if (!propertyDataSources.Any())
        {
            return combinations;
        }

        var propertyValueFactories = new Dictionary<string, Func<Task<object?>>>();

        foreach (var propertyDataSource in propertyDataSources)
        {
            try
            {
                var metadata = CreateDataGeneratorMetadata(global::TUnit.Core.Enums.DataGeneratorType.Property);

                // Get first data row for property
                var firstRow = await GetFirstDataRowAsync(propertyDataSource.DataSource, metadata);
                if (firstRow != null)
                {
                    var data = await firstRow();
                    if (data is { Length: > 0 })
                    {
                        var value = data[0];
                        propertyValueFactories[propertyDataSource.PropertyName] = () => Task.FromResult(value);
                    }
                }
            }
            catch (Exception ex)
            {
                // Property data generation failure is a configuration error that should fail the test
                throw new InvalidOperationException($"Failed to generate property data for {propertyDataSource.PropertyName}: {ex.Message}", ex);
            }
        }

        combinations.Add(new PropertyDataCombination
        {
            // Properties are now resolved directly via PropertyDataSources in PropertyInjector
        });

        return combinations;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072:Target method argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "Reflection mode cannot support AOT")]
    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern", Justification = "Reflection mode cannot support AOT")]
    private DataGeneratorMetadata CreateDataGeneratorMetadata(global::TUnit.Core.Enums.DataGeneratorType type)
    {
        // Create minimal metadata structures for reflection mode
        var classMetadata = ClassMetadata.GetOrAdd(_testClass.FullName ?? _testClass.Name, () => new ClassMetadata
        {
            Type = _testClass,
            Name = _testClass.Name,
            Namespace = _testClass.Namespace ?? string.Empty,
            TypeReference = new TypeReference { AssemblyQualifiedName = _testClass.AssemblyQualifiedName },
            Assembly = AssemblyMetadata.GetOrAdd(_testClass.Assembly.GetName().Name ?? "Unknown", () => new AssemblyMetadata
            {
                Name = _testClass.Assembly.GetName().Name ?? "Unknown"
            }),
            Parameters = [
            ],
            Properties = [
            ],
            Parent = null
        });

        var methodMetadata = new MethodMetadata
        {
            Name = _testMethod.Name,
            Type = _testMethod.DeclaringType ?? _testClass,
            Class = classMetadata,
            Parameters = _testMethod.GetParameters().Select((p, i) => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? $"param{i}",
                TypeReference = new TypeReference { AssemblyQualifiedName = p.ParameterType.AssemblyQualifiedName },
                ReflectionInfo = p
            }).ToArray(),
            GenericTypeCount = _testMethod.IsGenericMethodDefinition ? _testMethod.GetGenericArguments().Length : 0,
            ReturnTypeReference = new TypeReference { AssemblyQualifiedName = _testMethod.ReturnType.AssemblyQualifiedName },
            ReturnType = _testMethod.ReturnType,
            TypeReference = new TypeReference { AssemblyQualifiedName = (_testMethod.DeclaringType ?? _testClass).AssemblyQualifiedName }
        };

        // Filter out CancellationToken parameters for consistency with source generation mode
        var membersToGenerate = type == Core.Enums.DataGeneratorType.TestParameters
            ? methodMetadata.Parameters
            : [];

        // Debug: Log the method and its parameters
        System.Diagnostics.Debug.WriteLine($"[ReflectionTestMetadata] Method: {_testMethod.Name}, Type: {type}, Parameters: {membersToGenerate.Length}");
        for (int i = 0; i < membersToGenerate.Length; i++)
        {
            System.Diagnostics.Debug.WriteLine($"  Param[{i}]: {membersToGenerate[i].Name} : {membersToGenerate[i].Type}");
        }

        // Filter out CancellationToken if it's the last parameter (handled by the engine)
        if (type == Core.Enums.DataGeneratorType.TestParameters && membersToGenerate.Length > 0)
        {
            var lastParam = membersToGenerate[membersToGenerate.Length - 1];
            if (lastParam.Type == typeof(System.Threading.CancellationToken))
            {
                System.Diagnostics.Debug.WriteLine($"[ReflectionTestMetadata] Filtering out CancellationToken parameter from {_testMethod.Name}");
                var newArray = new ParameterMetadata[membersToGenerate.Length - 1];
                Array.Copy(membersToGenerate, 0, newArray, 0, membersToGenerate.Length - 1);
                membersToGenerate = newArray;
            }
        }

        System.Diagnostics.Debug.WriteLine($"[ReflectionTestMetadata] Final members count for {_testMethod.Name}: {membersToGenerate.Length}");

        return new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext
            {
                TestMetadata = null! // TODO
            }),
            MembersToGenerate = [..membersToGenerate],
            TestInformation = methodMetadata,
            Type = type,
            TestSessionId = "reflection-discovery",
            TestClassInstance = null,
            ClassInstanceArguments = null
        };
    }

    private async Task<Func<Task<object?[]?>>?> GetFirstDataRowAsync(IDataSourceAttribute dataSource, DataGeneratorMetadata metadata)
    {
        await foreach (var row in dataSource.GetDataRowsAsync(metadata))
        {
            return row;
        }
        return null;
    }
}
