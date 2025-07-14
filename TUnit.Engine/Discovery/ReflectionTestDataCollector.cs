using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using TUnit.Core;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Test data collector for Reflection mode - discovers tests at runtime using reflection
/// </summary>
[RequiresUnreferencedCode("Reflection-based test discovery requires unreferenced code")]
[RequiresDynamicCode("Expression compilation requires dynamic code generation")]
public sealed class ReflectionTestDataCollector : ITestDataCollector
{
    private static readonly HashSet<Assembly> _scannedAssemblies = new();
    private static readonly List<TestMetadata> _discoveredTests = new();
    private static readonly object _lock = new();
    private static bool _assemblyLoadHandlerRegistered;
    private static readonly ExpressionCacheService _expressionCache = new();

    public Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        lock (_lock)
        {
            if (!_assemblyLoadHandlerRegistered)
            {
                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;
                _assemblyLoadHandlerRegistered = true;
            }
        }

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(ShouldScanAssembly)
            .ToList();

        var newTests = new List<TestMetadata>();

        foreach (var assembly in assemblies)
        {
            lock (_lock)
            {
                if (!_scannedAssemblies.Add(assembly))
                {
                    continue;
                }
            }

            try
            {
                var testsInAssembly = DiscoverTestsInAssembly(assembly);
                newTests.AddRange(testsInAssembly);
            }
            catch (Exception ex)
            {
                // Log warning about assembly that couldn't be scanned
                Console.WriteLine($"Warning: Failed to scan assembly {assembly.FullName}: {ex.Message}");
            }
        }

        lock (_lock)
        {
            _discoveredTests.AddRange(newTests);
            
            // Log expression cache statistics
            if (DiscoveryDiagnostics.IsEnabled)
            {
                var stats = _expressionCache.GetCacheStatistics();
                DiscoveryDiagnostics.RecordEvent("ExpressionCacheStats", 
                    $"Instance Factories: {stats.InstanceFactories}, Test Invokers: {stats.TestInvokers}");
            }
            
            return Task.FromResult<IEnumerable<TestMetadata>>(_discoveredTests.ToList());
        }
    }

    private static void OnAssemblyLoaded(object? sender, AssemblyLoadEventArgs args)
    {
        if (!ShouldScanAssembly(args.LoadedAssembly))
        {
            return;
        }

        lock (_lock)
        {
            if (_scannedAssemblies.Contains(args.LoadedAssembly))
            {
                return;
            }

            _scannedAssemblies.Add(args.LoadedAssembly);
        }

        try
        {
            var tests = DiscoverTestsInAssembly(args.LoadedAssembly);
            lock (_lock)
            {
                _discoveredTests.AddRange(tests);
                
                // Log expression cache statistics for dynamically loaded assemblies
                if (DiscoveryDiagnostics.IsEnabled)
                {
                    var stats = _expressionCache.GetCacheStatistics();
                    DiscoveryDiagnostics.RecordEvent("ExpressionCacheStats (Dynamic)", 
                        $"Instance Factories: {stats.InstanceFactories}, Test Invokers: {stats.TestInvokers}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to scan dynamically loaded assembly {args.LoadedAssembly.FullName}: {ex.Message}");
        }
    }

    private static bool ShouldScanAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name == null)
        {
            return false;
        }

        // Skip system and framework assemblies
        if (name.StartsWith("System.") ||
            name.StartsWith("Microsoft.") ||
            name.StartsWith("netstandard") ||
            name.StartsWith("mscorlib"))
        {
            return false;
        }

        // Skip TUnit framework assemblies (except test projects)
        if ((name.StartsWith("TUnit.") && !name.Contains("Test")) ||
            name == "TUnit.Core" ||
            name == "TUnit.Engine" ||
            name == "TUnit.Assertions")
        {
            return false;
        }

        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Reflection mode cannot support trimming")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    private static IEnumerable<TestMetadata> DiscoverTestsInAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true });

        foreach (var type in types)
        {
            var testMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<TestAttribute>() != null);

            foreach (var method in testMethods)
            {
                TestMetadata? metadata = null;
                try
                {
                    metadata = BuildTestMetadata(type, method);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to build metadata for test {type.FullName}.{method.Name}: {ex.Message}");
                }

                if (metadata != null)
                {
                    yield return metadata;
                }
            }
        }
    }

    private static TestMetadata BuildTestMetadata(Type testClass, MethodInfo testMethod)
    {
        var testName = GenerateTestName(testClass, testMethod);

        var metadata = new ReflectionTestMetadata(testClass, testMethod)
        {
            TestName = testName,
            TestClassType = testClass,
            TestMethodName = testMethod.Name,
            Categories = ExtractCategories(testClass, testMethod),
            IsSkipped = IsTestSkipped(testClass, testMethod, out var skipReason),
            SkipReason = skipReason,
            TimeoutMs = ExtractTimeout(testClass, testMethod),
            RetryCount = ExtractRetryCount(testClass, testMethod),
            CanRunInParallel = CanRunInParallel(testClass, testMethod),
            Dependencies = ExtractDependencies(testClass, testMethod),
            DataSources = ExtractMethodDataSources(testMethod),
            ClassDataSources = ExtractClassDataSources(testClass),
            PropertyDataSources = ExtractPropertyDataSources(testClass),
            InstanceFactory = CreateInstanceFactory(testClass),
            TestInvoker = CreateTestInvoker(testClass, testMethod),
            ParameterCount = testMethod.GetParameters().Length,
            ParameterTypes = testMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
            TestMethodParameterTypes = testMethod.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name).ToArray(),
            Hooks = DiscoverHooks(testClass),
            FilePath = ExtractFilePath(testMethod),
            LineNumber = ExtractLineNumber(testMethod),
            GenericTypeInfo = ExtractGenericTypeInfo(testClass),
            GenericMethodInfo = ExtractGenericMethodInfo(testMethod),
            GenericMethodTypeArguments = testMethod.IsGenericMethodDefinition ? null : testMethod.GetGenericArguments(),
            AttributeFactory = () =>
            [
                ..testMethod.GetCustomAttributes(),
                ..testClass.GetCustomAttributes(),
                ..testClass.Assembly.GetCustomAttributes(),
            ],
            MethodMetadata = BuildMethodMetadata(testClass, testMethod)
        };

        return metadata;
    }

private static string GenerateTestName(Type testClass, MethodInfo testMethod)
    {
        return $"{testClass.Name}.{testMethod.Name}";
    }

    private static string[] ExtractCategories(Type testClass, MethodInfo testMethod)
    {
        var categories = new HashSet<string>();

        // Get categories from assembly
        var assemblyCategories = testClass.Assembly.GetCustomAttributes<CategoryAttribute>();
        foreach (var attr in assemblyCategories)
            categories.Add(attr.Category);

        // Get categories from class
        var classCategories = testClass.GetCustomAttributes<CategoryAttribute>();
        foreach (var attr in classCategories)
            categories.Add(attr.Category);

        // Get categories from method
        var methodCategories = testMethod.GetCustomAttributes<CategoryAttribute>();
        foreach (var attr in methodCategories)
            categories.Add(attr.Category);

        return categories.ToArray();
    }

    private static bool IsTestSkipped(Type testClass, MethodInfo testMethod, out string? skipReason)
    {
        // Check method-level skip
        var methodSkip = testMethod.GetCustomAttribute<SkipAttribute>();
        if (methodSkip != null)
        {
            skipReason = methodSkip.Reason;
            return true;
        }

        // Check class-level skip
        var classSkip = testClass.GetCustomAttribute<SkipAttribute>();
        if (classSkip != null)
        {
            skipReason = classSkip.Reason;
            return true;
        }

        // Check assembly-level skip
        var assemblySkip = testClass.Assembly.GetCustomAttribute<SkipAttribute>();
        if (assemblySkip != null)
        {
            skipReason = assemblySkip.Reason;
            return true;
        }

        skipReason = null;
        return false;
    }

    private static int? ExtractTimeout(Type testClass, MethodInfo testMethod)
    {
        // Check method-level timeout (highest priority)
        var methodTimeout = testMethod.GetCustomAttribute<TimeoutAttribute>();
        if (methodTimeout != null)
        {
            return (int)methodTimeout.Timeout.TotalMilliseconds;
        }

        // Check class-level timeout
        var classTimeout = testClass.GetCustomAttribute<TimeoutAttribute>();
        if (classTimeout != null)
        {
            return (int)classTimeout.Timeout.TotalMilliseconds;
        }

        // Check assembly-level timeout
        var assemblyTimeout = testClass.Assembly.GetCustomAttribute<TimeoutAttribute>();
        if (assemblyTimeout != null)
        {
            return (int)assemblyTimeout.Timeout.TotalMilliseconds;
        }

        return null;
    }

    private static int ExtractRetryCount(Type testClass, MethodInfo testMethod)
    {
        // Check method-level retry (highest priority)
        var methodRetry = testMethod.GetCustomAttribute<RetryAttribute>();
        if (methodRetry != null)
        {
            return methodRetry.Times;
        }

        // Check class-level retry
        var classRetry = testClass.GetCustomAttribute<RetryAttribute>();
        if (classRetry != null)
        {
            return classRetry.Times;
        }

        // Check assembly-level retry
        var assemblyRetry = testClass.Assembly.GetCustomAttribute<RetryAttribute>();
        if (assemblyRetry != null)
        {
            return assemblyRetry.Times;
        }

        return 0;
    }

    private static bool CanRunInParallel(Type testClass, MethodInfo testMethod)
    {
        // Check if NotInParallel attribute is present at any level
        if (testMethod.GetCustomAttribute<NotInParallelAttribute>() != null)
        {
            return false;
        }

        if (testClass.GetCustomAttribute<NotInParallelAttribute>() != null)
        {
            return false;
        }

        if (testClass.Assembly.GetCustomAttribute<NotInParallelAttribute>() != null)
        {
            return false;
        }

        return true;
    }

    private static TestDependency[] ExtractDependencies(Type testClass, MethodInfo testMethod)
    {
        var dependencies = new List<TestDependency>();

        // Get dependencies from method
        var methodDependencies = testMethod.GetCustomAttributes<DependsOnAttribute>();
        foreach (var attr in methodDependencies)
        {
            dependencies.Add(attr.ToTestDependency());
        }

        // Get dependencies from class
        var classDependencies = testClass.GetCustomAttributes<DependsOnAttribute>();
        foreach (var attr in classDependencies)
        {
            dependencies.Add(attr.ToTestDependency());
        }

        return dependencies.ToArray();
    }

    private static TestDataSource[] ExtractMethodDataSources(MethodInfo testMethod)
    {
        var dataSources = new List<TestDataSource>();

        // Get Arguments attributes
        var argumentsAttrs = testMethod.GetCustomAttributes<ArgumentsAttribute>();
        foreach (var attr in argumentsAttrs)
        {
            dataSources.Add(new StaticTestDataSource([attr.Values]));
        }

        // Get MethodDataSource attributes
        var methodDataAttrs = testMethod.GetCustomAttributes<MethodDataSourceAttribute>();
        foreach (var attr in methodDataAttrs)
        {
            var dataSource = CreateMethodDataSource(attr, testMethod.DeclaringType!);
            if (dataSource != null)
            {
                dataSources.Add(dataSource);
            }
        }

        // Get ClassDataSource attributes
        var classDataAttrs = testMethod.GetCustomAttributes<ClassDataSourceAttribute>();
        foreach (var attr in classDataAttrs)
        {
            var dataSource = CreateClassDataSource(attr);
            if (dataSource != null)
            {
                dataSources.Add(dataSource);
            }
        }

        return dataSources.ToArray();
    }

    private static TestDataSource[] ExtractClassDataSources(Type testClass)
    {
        var dataSources = new List<TestDataSource>();

        // Get Arguments attributes on class
        var argumentsAttrs = testClass.GetCustomAttributes<ArgumentsAttribute>();
        foreach (var attr in argumentsAttrs)
        {
            dataSources.Add(new StaticTestDataSource([attr.Values]));
        }

        // Get MethodDataSource attributes on class
        var methodDataAttrs = testClass.GetCustomAttributes<MethodDataSourceAttribute>();
        foreach (var attr in methodDataAttrs)
        {
            var dataSource = CreateMethodDataSource(attr, testClass);
            if (dataSource != null)
            {
                dataSources.Add(dataSource);
            }
        }

        // Get ClassDataSource attributes on class
        var classDataAttrs = testClass.GetCustomAttributes<ClassDataSourceAttribute>();
        foreach (var attr in classDataAttrs)
        {
            var dataSource = CreateClassDataSource(attr);
            if (dataSource != null)
            {
                dataSources.Add(dataSource);
            }
        }

        return dataSources.ToArray();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicProperties' in call to 'System.Type.GetProperties(BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    private static PropertyDataSource[] ExtractPropertyDataSources([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass)
    {
        var propertyDataSources = new List<PropertyDataSource>();

        var properties = testClass.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            var dataSources = new List<TestDataSource>();

            // Get Arguments attributes on property
            var argumentsAttrs = property.GetCustomAttributes<ArgumentsAttribute>();
            foreach (var attr in argumentsAttrs)
            {
                dataSources.Add(new StaticTestDataSource([attr.Values]));
            }

            // Get MethodDataSource attributes on property
            var methodDataAttrs = property.GetCustomAttributes<MethodDataSourceAttribute>();
            foreach (var attr in methodDataAttrs)
            {
                var dataSource = CreateMethodDataSource(attr, testClass);
                if (dataSource != null)
                {
                    dataSources.Add(dataSource);
                }
            }

            // Get ClassDataSource attributes on property
            var classDataAttrs = property.GetCustomAttributes<ClassDataSourceAttribute>();
            foreach (var attr in classDataAttrs)
            {
                var dataSource = CreateClassDataSource(attr);
                if (dataSource != null)
                {
                    dataSources.Add(dataSource);
                }
            }

            // If property has data sources, create a PropertyDataSource for each
            foreach (var dataSource in dataSources)
            {
                propertyDataSources.Add(new PropertyDataSource
                {
                    PropertyName = property.Name,
                    PropertyType = property.PropertyType,
                    DataSource = dataSource
                });
            }
        }

        return propertyDataSources.ToArray();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.NonPublicMethods' in call to 'System.Type.GetMethod(String, BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods', 'DynamicallyAccessedMemberTypes.NonPublicMethods' in call to 'System.Type.GetMethod(String, BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2067:'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'System.Activator.CreateInstance(Type)'", Justification = "Reflection mode requires dynamic access")]
    private static TestDataSource? CreateMethodDataSource(MethodDataSourceAttribute attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type defaultClass)
    {
        var targetClass = attr.ClassProvidingDataSource ?? defaultClass;
        var method = targetClass.GetMethod(attr.MethodNameProvidingDataSource,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        if (method == null)
        {
            Console.WriteLine($"Warning: Method {attr.MethodNameProvidingDataSource} not found on type {targetClass.FullName}");
            return null;
        }

        // Check if method is static or if we need an instance
        object? instance = null;
        if (!method.IsStatic)
        {
            try
            {
                instance = Activator.CreateInstance(targetClass);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Cannot create instance of {targetClass.FullName} for data source: {ex.Message}");
                return null;
            }
        }

        // Determine return type and create appropriate data source
        var returnType = method.ReturnType;

        if (returnType == typeof(Task<IEnumerable<object?[]>>))
        {
            var factory = CreateTaskDataSourceFactory(method, instance, attr.Arguments);
            return new TaskDelegateDataSource(factory);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            var factory = CreateAsyncDataSourceFactory(method, instance, attr.Arguments);
            return new AsyncDelegateDataSource(factory);
        }
        else if (typeof(IEnumerable<object?[]>).IsAssignableFrom(returnType))
        {
            var factory = CreateSyncDataSourceFactory(method, instance, attr.Arguments);
            return new DelegateDataSource(factory);
        }
        else
        {
            // Handle methods that return tuples or single values
            var factory = CreateWrappedDataSourceFactory(method, instance, attr.Arguments);
            return new DelegateDataSource(factory);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072:'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'System.Activator.CreateInstance(Type)'", Justification = "Reflection mode requires dynamic access")]
    private static TestDataSource? CreateClassDataSource(ClassDataSourceAttribute attr)
    {
        // ClassDataSource creates instances of the specified type
        var dataType = attr.GetType().GetGenericArguments().FirstOrDefault();
        if (dataType == null)
        {
            return null;
        }

        try
        {
            var instance = Activator.CreateInstance(dataType);
            return new StaticTestDataSource([[instance]]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Cannot create instance of {dataType.FullName} for class data source: {ex.Message}");
            return null;
        }
    }

    private static Func<IEnumerable<object?[]>> CreateSyncDataSourceFactory(MethodInfo method, object? instance, object?[] args)
    {
        return () =>
        {
            var result = method.Invoke(instance, args);
            if (result is IEnumerable<object?[]> enumerable)
            {
                return enumerable;
            }
            return [];
        };
    }

    private static Func<Task<IEnumerable<object?[]>>> CreateTaskDataSourceFactory(MethodInfo method, object? instance, object?[] args)
    {
        return () =>
        {
            var result = method.Invoke(instance, args);
            if (result is Task<IEnumerable<object?[]>> task)
            {
                return task;
            }
            return Task.FromResult(Enumerable.Empty<object?[]>());
        };
    }

    private static Func<CancellationToken, IAsyncEnumerable<object?[]>> CreateAsyncDataSourceFactory(MethodInfo method, object? instance, object?[] args)
    {
        return (cancellationToken) =>
        {
            var result = method.Invoke(instance, args);
            if (result is IAsyncEnumerable<object?[]> asyncEnumerable)
            {
                return asyncEnumerable;
            }
            return EmptyAsyncEnumerable();
        };
    }

    private static Func<IEnumerable<object?[]>> CreateWrappedDataSourceFactory(MethodInfo method, object? instance, object?[] args)
    {
        return () =>
        {
            var result = method.Invoke(instance, args);

            // If result is a tuple or single value, wrap it
            if (result != null && result.GetType().Name.StartsWith("ValueTuple"))
            {
                // Extract tuple values
                var tupleType = result.GetType();
                var fields = tupleType.GetFields();
                var values = fields.Select(f => f.GetValue(result)).ToArray();
                return [[values]];
            }

            // Single value
            return [[result]];
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' in call to 'System.Type.GetConstructors()'", Justification = "Reflection mode requires dynamic access")]
    private static Func<object?[], object>? CreateInstanceFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type testClass)
    {
        var constructors = testClass.GetConstructors();
        if (constructors.Length == 0)
        {
            return null;
        }

        var ctor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0)
                   ?? constructors.First();

        return _expressionCache.GetOrCreateInstanceFactory(ctor, CompileInstanceFactory);
    }

    private static Func<object?[], object> CompileInstanceFactory(ConstructorInfo ctor)
    {
        var parameters = ctor.GetParameters();
        var paramExpr = Expression.Parameter(typeof(object[]), "args");

        var argExpressions = new Expression[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var indexExpr = Expression.ArrayIndex(paramExpr, Expression.Constant(i));
            var convertExpr = Expression.Convert(indexExpr, parameters[i].ParameterType);
            argExpressions[i] = convertExpr;
        }

        var newExpr = Expression.New(ctor, argExpressions);
        var lambdaExpr = Expression.Lambda<Func<object?[], object>>(
            Expression.Convert(newExpr, typeof(object)),
            paramExpr);

        return lambdaExpr.Compile();
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Using member 'System.Linq.Expressions.Expression.Call(Type, String, Type[], params Expression[])' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling", Justification = "Reflection mode cannot support AOT")]
    private static Func<object, object?[], Task> CreateTestInvoker(Type testClass, MethodInfo testMethod)
    {
        return _expressionCache.GetOrCreateTestInvoker(testClass, testMethod, 
            key => CompileTestInvoker(key.Item1, key.Item2));
    }

    private static Func<object, object?[], Task> CompileTestInvoker(Type testClass, MethodInfo testMethod)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var argsParam = Expression.Parameter(typeof(object[]), "args");

        var instanceExpr = testMethod.IsStatic
            ? null
            : Expression.Convert(instanceParam, testClass);

        var parameters = testMethod.GetParameters();
        var argExpressions = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var indexExpr = Expression.ArrayIndex(argsParam, Expression.Constant(i));
            var convertExpr = Expression.Convert(indexExpr, parameters[i].ParameterType);
            argExpressions[i] = convertExpr;
        }

        var callExpr = testMethod.IsStatic
            ? Expression.Call(testMethod, argExpressions)
            : Expression.Call(instanceExpr!, testMethod, argExpressions);

        Expression body;
        if (testMethod.ReturnType == typeof(Task))
        {
            body = callExpr;
        }
        else if (testMethod.ReturnType == typeof(void))
        {
            var blockExpr = Expression.Block(
                callExpr,
                Expression.Constant(Task.CompletedTask)
            );
            body = blockExpr;
        }
        else if (testMethod.ReturnType.IsGenericType &&
                 testMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            body = Expression.Call(
                typeof(ReflectionTestDataCollector),
                nameof(ConvertToNonGenericTask),
                null,
                callExpr);
        }
        else if (testMethod.ReturnType == typeof(ValueTask))
        {
            body = Expression.Call(
                callExpr,
                typeof(ValueTask).GetMethod("AsTask")!);
        }
        else
        {
            // Sync method returning a value
            var blockExpr = Expression.Block(
                callExpr,
                Expression.Constant(Task.CompletedTask)
            );
            body = blockExpr;
        }

        var lambda = Expression.Lambda<Func<object, object?[], Task>>(
            body,
            instanceParam,
            argsParam);

        return lambda.Compile();
    }

    private static Task ConvertToNonGenericTask<T>(Task<T> task)
    {
        return task;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Using member 'System.Reflection.Assembly.GetTypes()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code", Justification = "Reflection mode cannot support trimming")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    private static TestHooks DiscoverHooks([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type testClass)
    {
        var beforeClassHooks = new List<HookMetadata>();
        var afterClassHooks = new List<HookMetadata>();
        var beforeTestHooks = new List<HookMetadata>();
        var afterTestHooks = new List<HookMetadata>();

        // Discover assembly-level hooks
        var assemblyMethods = testClass.Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            .ToList();

        // Discover class and test level hooks
        var classMethods = GetMethodsFromClassHierarchy(testClass);

        foreach (var method in classMethods.Concat(assemblyMethods).Distinct())
        {
            var beforeAttr = method.GetCustomAttribute<BeforeAttribute>();
            if (beforeAttr != null)
            {
                var hookMetadata = CreateHookMetadata(method, beforeAttr, isBeforeHook: true);
                if (hookMetadata != null)
                {
                    switch (beforeAttr.HookType)
                    {
                        case HookType.Class:
                            beforeClassHooks.Add(hookMetadata);
                            break;
                        case HookType.Test:
                            beforeTestHooks.Add(hookMetadata);
                            break;
                    }
                }
            }

            var afterAttr = method.GetCustomAttribute<AfterAttribute>();
            if (afterAttr != null)
            {
                var hookMetadata = CreateHookMetadata(method, afterAttr, isBeforeHook: false);
                if (hookMetadata != null)
                {
                    switch (afterAttr.HookType)
                    {
                        case HookType.Class:
                            afterClassHooks.Add(hookMetadata);
                            break;
                        case HookType.Test:
                            afterTestHooks.Add(hookMetadata);
                            break;
                    }
                }
            }
        }

        return new TestHooks
        {
            BeforeClass = beforeClassHooks.OrderBy(h => h.Order).ToArray(),
            AfterClass = afterClassHooks.OrderBy(h => h.Order).ToArray(),
            BeforeTest = beforeTestHooks.OrderBy(h => h.Order).ToArray(),
            AfterTest = afterTestHooks.OrderBy(h => h.Order).ToArray()
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods', 'DynamicallyAccessedMemberTypes.NonPublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods', 'DynamicallyAccessedMemberTypes.NonPublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    private static IEnumerable<MethodInfo> GetMethodsFromClassHierarchy([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        var methods = new List<MethodInfo>();
        var currentType = type;

        while (currentType != null && currentType != typeof(object))
        {
            methods.AddRange(currentType.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly));
            currentType = currentType.BaseType;
        }

        return methods;
    }

    private static HookMetadata? CreateHookMetadata(MethodInfo method, HookAttribute hookAttr, bool isBeforeHook)
    {
        // Hook invokers are no longer supported in reflection mode
        // Hooks require source-generated context-specific delegates
        return null;
    }

    // CreateHookInvoker removed - hooks are no longer supported in reflection mode
    // Hooks require source-generated context-specific delegates
    /*
    [UnconditionalSuppressMessage("Trimming", "IL2026:Using member 'System.Linq.Expressions.Expression.Property(Expression, String)' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code", Justification = "Reflection mode cannot support trimming")]
    private static Func<object?, HookContext, Task>? CreateHookInvoker(MethodInfo method)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var contextParam = Expression.Parameter(typeof(HookContext), "context");

        // Check parameters
        var parameters = method.GetParameters();
        if (parameters.Length > 1)
        {
            Console.WriteLine($"Warning: Hook method {method.Name} has too many parameters");
            return null;
        }

        Expression? callExpr;
        if (parameters.Length == 0)
        {
            // No parameters
            callExpr = method.IsStatic
                ? Expression.Call(method)
                : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method);
        }
        else if (parameters[0].ParameterType == typeof(HookContext) ||
                 parameters[0].ParameterType.IsAssignableFrom(typeof(HookContext)))
        {
            // HookContext parameter
            callExpr = method.IsStatic
                ? Expression.Call(method, contextParam)
                : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method, contextParam);
        }
        else if (parameters[0].ParameterType == typeof(TestContext))
        {
            // TestContext parameter - need to extract from HookContext
            var testContextProp = Expression.Property(contextParam, nameof(HookContext.TestContext));
            callExpr = method.IsStatic
                ? Expression.Call(method, testContextProp)
                : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method, testContextProp);
        }
        else
        {
            Console.WriteLine($"Warning: Hook method {method.Name} has unsupported parameter type {parameters[0].ParameterType}");
            return null;
        }

        // Convert return type to Task
        Expression body;
        if (method.ReturnType == typeof(Task))
        {
            body = callExpr;
        }
        else if (method.ReturnType == typeof(ValueTask))
        {
            body = Expression.Call(callExpr, typeof(ValueTask).GetMethod("AsTask")!);
        }
        else if (method.ReturnType == typeof(void))
        {
            body = Expression.Block(
                callExpr,
                Expression.Constant(Task.CompletedTask)
            );
        }
        else
        {
            Console.WriteLine($"Warning: Hook method {method.Name} has unsupported return type {method.ReturnType}");
            return null;
        }

        var lambda = Expression.Lambda<Func<object?, HookContext, Task>>(
            body,
            instanceParam,
            contextParam);

        return lambda.Compile();
    }
    */

    private static bool IsAsyncMethod(MethodInfo method)
    {
        return method.ReturnType == typeof(Task) ||
               method.ReturnType == typeof(ValueTask) ||
               (method.ReturnType.IsGenericType &&
                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
    }

    private static string? ExtractFilePath(MethodInfo method)
    {
        method.GetCustomAttribute<TestAttribute>();
        // Reflection doesn't have access to file path from CallerFilePath
        return null;
    }

    private static int? ExtractLineNumber(MethodInfo method)
    {
        method.GetCustomAttribute<TestAttribute>();
        // Reflection doesn't have access to line number from CallerLineNumber
        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2065:Value passed to implicit 'this' parameter of method 'System.Type.GetInterfaces()' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements", Justification = "Reflection mode requires dynamic access")]
    private static GenericTypeInfo? ExtractGenericTypeInfo(Type testClass)
    {
        if (!testClass.IsGenericTypeDefinition)
        {
            return null;
        }

        var genericParams = testClass.GetGenericArguments();
        var constraints = new GenericParameterConstraints[genericParams.Length];

        for (var i = 0; i < genericParams.Length; i++)
        {
            var param = genericParams[i];
            constraints[i] = new GenericParameterConstraints
            {
                ParameterName = param.Name,
                BaseTypeConstraint = param.BaseType != typeof(object) ? param.BaseType : null,
                InterfaceConstraints = param.GetInterfaces(),
                HasDefaultConstructorConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint),
                HasReferenceTypeConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint),
                HasValueTypeConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint),
                HasNotNullConstraint = false // .NET doesn't expose this via reflection
            };
        }

        return new GenericTypeInfo
        {
            ParameterNames = genericParams.Select(p => p.Name).ToArray(),
            Constraints = constraints
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2065:Value passed to implicit 'this' parameter of method 'System.Type.GetInterfaces()' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements", Justification = "Reflection mode requires dynamic access")]
    private static GenericMethodInfo? ExtractGenericMethodInfo(MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
        {
            return null;
        }

        var genericParams = method.GetGenericArguments();
        var constraints = new GenericParameterConstraints[genericParams.Length];
        var parameterPositions = new List<int>();

        // Map generic parameters to method argument positions
        var methodParams = method.GetParameters();
        for (var i = 0; i < methodParams.Length; i++)
        {
            var paramType = methodParams[i].ParameterType;
            if (paramType.IsGenericParameter && Array.IndexOf(genericParams, paramType) >= 0)
            {
                parameterPositions.Add(i);
            }
        }

        for (var i = 0; i < genericParams.Length; i++)
        {
            var param = genericParams[i];
            constraints[i] = new GenericParameterConstraints
            {
                ParameterName = param.Name,
                BaseTypeConstraint = param.BaseType != typeof(object) ? param.BaseType : null,
                InterfaceConstraints = param.GetInterfaces(),
                HasDefaultConstructorConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint),
                HasReferenceTypeConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint),
                HasValueTypeConstraint = param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint),
                HasNotNullConstraint = false // .NET doesn't expose this via reflection
            };
        }

        return new GenericMethodInfo
        {
            ParameterNames = genericParams.Select(p => p.Name).ToArray(),
            Constraints = constraints,
            ParameterPositions = parameterPositions.ToArray()
        };
    }

    private static async IAsyncEnumerable<object?[]> EmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static MethodMetadata BuildMethodMetadata(Type testClass, MethodInfo testMethod)
    {
        var parameters = testMethod.GetParameters()
            .Select(p => new ParameterMetadata<object>
            {
                Name = p.Name ?? string.Empty,
                TypeReference = new TypeReference
                {
                    AssemblyQualifiedName = p.ParameterType.AssemblyQualifiedName,
                    IsGenericParameter = p.ParameterType.IsGenericParameter,
                    GenericParameterPosition = p.ParameterType.IsGenericParameter ? p.ParameterType.GenericParameterPosition : 0,
                    IsMethodGenericParameter = p.ParameterType.IsGenericParameter && p.ParameterType.DeclaringMethod != null,
                    GenericParameterName = p.ParameterType.IsGenericParameter ? p.ParameterType.Name : null
                },
                ReflectionInfo = p
            })
            .Cast<ParameterMetadata>()
            .ToArray();

        var classMetadata = new ClassMetadata
        {
            Type = testClass,
            TypeReference = new TypeReference
            {
                AssemblyQualifiedName = testClass.AssemblyQualifiedName,
                IsGenericParameter = testClass.IsGenericParameter,
                GenericParameterPosition = testClass.IsGenericParameter ? testClass.GenericParameterPosition : 0,
                IsMethodGenericParameter = false,
                GenericParameterName = testClass.IsGenericParameter ? testClass.Name : null
            },
            Name = testClass.Name,
            Namespace = testClass.Namespace ?? string.Empty,
            Assembly = new AssemblyMetadata
            {
                Name = testClass.Assembly.GetName().Name ?? string.Empty
            },
            Parent = null,
            Parameters = Array.Empty<ParameterMetadata>(),
            Properties = Array.Empty<PropertyMetadata>()
        };

        return new MethodMetadata
        {
            Type = testClass,
            TypeReference = new TypeReference
            {
                AssemblyQualifiedName = testClass.AssemblyQualifiedName,
                IsGenericParameter = testClass.IsGenericParameter,
                GenericParameterPosition = testClass.IsGenericParameter ? testClass.GenericParameterPosition : 0,
                IsMethodGenericParameter = false,
                GenericParameterName = testClass.IsGenericParameter ? testClass.Name : null
            },
            Name = testMethod.Name,
            GenericTypeCount = testMethod.GetGenericArguments().Length,
            ReturnType = testMethod.ReturnType,
            ReturnTypeReference = new TypeReference
            {
                AssemblyQualifiedName = testMethod.ReturnType.AssemblyQualifiedName,
                IsGenericParameter = testMethod.ReturnType.IsGenericParameter,
                GenericParameterPosition = testMethod.ReturnType.IsGenericParameter ? testMethod.ReturnType.GenericParameterPosition : 0,
                IsMethodGenericParameter = testMethod.ReturnType.IsGenericParameter && testMethod.ReturnType.DeclaringMethod != null,
                GenericParameterName = testMethod.ReturnType.IsGenericParameter ? testMethod.ReturnType.Name : null
            },
            Parameters = parameters,
            Class = classMetadata
        };
    }
}
