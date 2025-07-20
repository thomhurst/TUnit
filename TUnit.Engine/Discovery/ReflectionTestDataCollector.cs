using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;
using TUnit.Core.Data;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Test data collector for Reflection mode - discovers tests at runtime using reflection
/// </summary>
[RequiresUnreferencedCode("Reflection-based test discovery requires unreferenced code")]
[RequiresDynamicCode("Expression compilation requires dynamic code generation")]
public sealed class ReflectionTestDataCollector : ITestDataCollector
{
    private static readonly HashSet<Assembly> _scannedAssemblies =
    [
    ];
    private static readonly List<TestMetadata> _discoveredTests =
    [
    ];
    private static readonly object _lock = new();
    private static readonly ExpressionCacheService _expressionCache = new();

    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        // Disable assembly loading event handler to prevent recursive issues
        // This was causing problems when assemblies were loaded during scanning

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(ShouldScanAssembly)
            .ToList();

        Console.WriteLine($"Scanning {assemblies.Count} assemblies for tests...");

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
                Console.WriteLine($"Scanning assembly: {assembly.GetName().Name}");
                var testsInAssembly = await DiscoverTestsInAssembly(assembly);
                newTests.AddRange(testsInAssembly);
            }
            catch (Exception ex)
            {
                // Create a failed test metadata for the assembly that couldn't be scanned
                var failedTest = CreateFailedTestMetadataForAssembly(assembly, ex);
                newTests.Add(failedTest);
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

            return _discoveredTests.ToList();
        }
    }

    private static IEnumerable<MethodInfo> GetAllTestMethods(Type type)
    {
        var methods = new List<MethodInfo>();
        var currentType = type;

        while (currentType != null && currentType != typeof(object))
        {
            methods.AddRange(currentType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
            currentType = currentType.BaseType;
        }

        return methods;
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
            var tests = Task.Run(async () => await DiscoverTestsInAssembly(args.LoadedAssembly)).Result;

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

        // Skip system and framework assemblies more aggressively
        if (name.StartsWith("System.") ||
            name.StartsWith("Microsoft.") ||
            name.StartsWith("netstandard") ||
            name.StartsWith("mscorlib") ||
            name.StartsWith("Windows.") ||
            name.StartsWith("PresentationFramework") ||
            name.StartsWith("PresentationCore") ||
            name.StartsWith("WindowsBase") ||
            name.StartsWith("Accessibility") ||
            name.StartsWith("DirectWriteForwarder") ||
            name.StartsWith("SMDiagnostics") ||
            name.StartsWith("System") ||
            name.StartsWith("Microsoft") ||
            name.StartsWith("NuGet.") ||
            name.StartsWith("Newtonsoft.") ||
            name.StartsWith("Castle.") ||
            name.StartsWith("Moq") ||
            name.StartsWith("xunit") ||
            name.StartsWith("nunit") ||
            name.StartsWith("FluentAssertions") ||
            name.StartsWith("AutoFixture") ||
            name.StartsWith("FakeItEasy") ||
            name.StartsWith("Shouldly") ||
            name.StartsWith("NSubstitute") ||
            name.StartsWith("Rhino.Mocks") ||
            name.StartsWith("testhost") ||
            name.StartsWith("MSTest") ||
            name.StartsWith("vstest") ||
            name.StartsWith("Microsoft.TestPlatform") ||
            name.StartsWith("Microsoft.Testing.Platform") ||
            name.StartsWith("anonymously") ||
            name.Contains("resources") ||
            name.Contains("resources.dll") ||
            name.Contains("XmlSerializers") ||
            name.EndsWith(".resources") ||
            name.EndsWith(".XmlSerializers"))
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

        // Skip assemblies that are likely to cause issues
        if (assembly.IsDynamic)
        {
            return false;
        }

        // Skip assemblies in certain locations (single-file apps will have empty locations)
        try
        {
#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
            var location = assembly.Location;
#pragma warning restore IL3000 // Avoid accessing Assembly file path when publishing as a single file
            if (!string.IsNullOrEmpty(location) &&
                (location.Contains("ref") ||
                 location.Contains("runtimes") ||
                 location.Contains("Microsoft.NETCore.App") ||
                 location.Contains("Microsoft.AspNetCore.App") ||
                 location.Contains("Microsoft.WindowsDesktop.App")))
            {
                return false;
            }
        }
        catch
        {
            // If we can't get the location, skip this assembly to be safe
            return false;
        }

        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Reflection mode cannot support trimming")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    private static async Task<List<TestMetadata>> DiscoverTestsInAssembly(Assembly assembly)
    {
        var discoveredTests = new List<TestMetadata>();

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to get types from assembly {assembly.FullName}: {ex.Message}");
            return discoveredTests;
        }

        // Include both concrete classes and abstract classes (for inherited tests)
        // Exclude compiler-generated types
        var filteredTypes = types.Where(t => t.IsClass && !IsCompilerGenerated(t));

        Console.WriteLine($"Checking {filteredTypes.Count()} types...");

        foreach (var type in filteredTypes)
        {
            // Skip abstract types - they can't be instantiated
            if (type.IsAbstract)
            {
                continue;
            }

            // Handle generic type definitions specially
            if (type.IsGenericTypeDefinition)
            {
                var genericTests = await DiscoverGenericTests(type);
                discoveredTests.AddRange(genericTests);
                continue;
            }

            MethodInfo[] testMethods;
            try
            {
                // Check if this class inherits tests from base classes
                var inheritsTests = type.GetCustomAttribute<InheritsTestsAttribute>() != null;

                if (inheritsTests)
                {
                    // Get all methods including inherited ones
                    testMethods = GetAllTestMethods(type)
                        .Where(m => m.GetCustomAttribute<TestAttribute>() != null && !m.IsAbstract)
                        .ToArray();
                }
                else
                {
                    // Only get declared methods
                    testMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(m => m.GetCustomAttribute<TestAttribute>() != null && !m.IsAbstract)
                        .ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to get methods from type {type.FullName}: {ex.Message}");
                continue;
            }

            foreach (var method in testMethods)
            {
                try
                {
                    var expandedTests = await BuildExpandedTestMetadata(type, method);
                    discoveredTests.AddRange(expandedTests);
                }
                catch (Exception ex)
                {
                    // Create a failed test metadata for this specific test
                    var failedTest = CreateFailedTestMetadata(type, method, ex);
                    discoveredTests.Add(failedTest);
                }
            }
        }

        return discoveredTests;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2055:Call to 'System.Type.MakeGenericType' can not be statically analyzed", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2067:'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'System.Activator.CreateInstance(Type)'", Justification = "Reflection mode requires dynamic access")]
    private static async Task<List<TestMetadata>> DiscoverGenericTests(Type genericTypeDefinition)
    {
        var discoveredTests = new List<TestMetadata>();

        // Extract class-level data sources that will determine the generic type arguments
        var classDataSources = ExtractClassDataSources(genericTypeDefinition);

        if (classDataSources.Length == 0)
        {
            // This is expected for generic test classes in reflection mode
            // They need data sources to determine concrete types
            return discoveredTests;
        }

        // Get test methods from the generic type definition
        var testMethods = genericTypeDefinition.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<TestAttribute>() != null && !m.IsAbstract)
            .ToArray();

        if (testMethods.Length == 0)
        {
            return discoveredTests;
        }

        // For each data source combination, create a concrete generic type
        foreach (var dataSource in classDataSources)
        {
            var dataItems = await GetDataFromSourceAsync(dataSource);

            foreach (var dataRow in dataItems)
            {
                if (dataRow == null || dataRow.Length == 0)
                {
                    continue;
                }

                // Determine generic type arguments from the data
                var typeArguments = DetermineGenericTypeArguments(genericTypeDefinition, dataRow);
                if (typeArguments == null || typeArguments.Length == 0)
                {
                    continue;
                }

                try
                {
                    // Create concrete type
                    var concreteType = genericTypeDefinition.MakeGenericType(typeArguments);

                    // Build tests for each method in the concrete type
                    foreach (var genericMethod in testMethods)
                    {
                        var concreteMethod = concreteType.GetMethod(genericMethod.Name,
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

                        if (concreteMethod != null)
                        {
                            var expandedTests = await BuildExpandedTestMetadata(concreteType, concreteMethod);
                            discoveredTests.AddRange(expandedTests);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to create concrete type for {genericTypeDefinition.Name}: {ex.Message}", ex);
                }
            }
        }

        return discoveredTests;
    }

    private static Type[] DetermineGenericTypeArguments(Type genericTypeDefinition, object?[] dataRow)
    {
        var genericParameters = genericTypeDefinition.GetGenericArguments();
        var typeArguments = new Type[genericParameters.Length];

        // For each generic parameter, determine the concrete type from the data
        for (int i = 0; i < genericParameters.Length && i < dataRow.Length; i++)
        {
            if (dataRow[i] != null)
            {
                typeArguments[i] = dataRow[i]!.GetType();
            }
            else
            {
                // If data is null, we can't determine the type
                // Use object as a fallback
                typeArguments[i] = typeof(object);
            }
        }

        return typeArguments;
    }

    private static async Task<List<object?[]>> GetDataFromSourceAsync(IDataSourceAttribute dataSource)
    {
        var data = new List<object?[]>();

        try
        {
            // Create minimal metadata for discovery
            var metadata = new DataGeneratorMetadata
            {
                TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
                MembersToGenerate = [
                ],
                TestInformation = new MethodMetadata
                {
                    Name = "Discovery",
                    Type = typeof(object),
                    Class = new ClassMetadata
                    {
                        Name = "Discovery",
                        Type = typeof(object),
                        Namespace = string.Empty,
                        TypeReference = new TypeReference { AssemblyQualifiedName = typeof(object).AssemblyQualifiedName },
                        Assembly = AssemblyMetadata.GetOrAdd("Discovery", () => new AssemblyMetadata { Name = "Discovery" }),
                        Parameters = [
                        ],
                        Properties = [
                        ],
                        Parent = null
                    },
                    Parameters = [
                    ],
                    GenericTypeCount = 0,
                    ReturnTypeReference = new TypeReference { AssemblyQualifiedName = typeof(void).AssemblyQualifiedName },
                    ReturnType = typeof(void),
                    TypeReference = new TypeReference { AssemblyQualifiedName = typeof(object).AssemblyQualifiedName }
                },
                Type = global::TUnit.Core.Enums.DataGeneratorType.ClassParameters,
                TestSessionId = "discovery",
                TestClassInstance = null,
                ClassInstanceArguments = null
            };
            
            // Get data rows from the source
            await foreach (var rowFactory in dataSource.GetDataRowsAsync(metadata))
            {
                var dataArray = await rowFactory();
                if (dataArray != null)
                {
                    data.Add(dataArray);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to get data from source: {ex.Message}", ex);
        }

        return await Task.FromResult(data);
    }

    private static async Task<List<TestMetadata>> BuildExpandedTestMetadata(Type testClass, MethodInfo testMethod)
    {
        // Create a base ReflectionTestMetadata instance
        var testName = GenerateTestName(testClass, testMethod);
        var baseMetadata = new ReflectionTestMetadata(testClass, testMethod)
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
            MethodMetadata = BuildMethodMetadata(testClass, testMethod),
            GenericTypeInfo = ExtractGenericTypeInfo(testClass),
            GenericMethodInfo = ExtractGenericMethodInfo(testMethod),
            GenericMethodTypeArguments = testMethod.IsGenericMethodDefinition ? null : testMethod.GetGenericArguments(),
            AttributeFactory = () => testMethod.GetCustomAttributes().Concat(testClass.GetCustomAttributes()).ToArray(),
            PropertyInjections = PropertyInjector.DiscoverInjectableProperties(testClass)
        };

        // Check if we should expand data sources during discovery
        var hasDataSources = baseMetadata.DataSources.Length > 0 ||
                           baseMetadata.ClassDataSources.Length > 0 ||
                           baseMetadata.PropertyDataSources.Length > 0;


        // Only expand if there are data sources
        if (!hasDataSources)
        {
            return
            [
                baseMetadata
            ];
        }

        // Expand data sources to create individual test metadata for each combination
        var expandedMetadata = new List<TestMetadata>();

        try
        {
            // Get all data combinations from the base metadata
            var dataCombinationGenerator = baseMetadata.DataCombinationGenerator();
            var combinationIndex = 0;

            await foreach (var combination in dataCombinationGenerator)
            {
                // Create a new metadata instance for each combination
                var expandedTest = new ExpandedReflectionTestMetadata(testClass, testMethod, combination, combinationIndex++)
                {
                    TestName = testName,
                    TestClassType = baseMetadata.TestClassType,
                    TestMethodName = baseMetadata.TestMethodName,
                    Categories = baseMetadata.Categories,
                    IsSkipped = baseMetadata.IsSkipped,
                    SkipReason = baseMetadata.SkipReason,
                    TimeoutMs = baseMetadata.TimeoutMs,
                    RetryCount = baseMetadata.RetryCount,
                    CanRunInParallel = baseMetadata.CanRunInParallel,
                    Dependencies = baseMetadata.Dependencies,
                    DataSources = baseMetadata.DataSources,
                    ClassDataSources = baseMetadata.ClassDataSources,
                    PropertyDataSources = baseMetadata.PropertyDataSources,
                    InstanceFactory = baseMetadata.InstanceFactory,
                    TestInvoker = baseMetadata.TestInvoker,
                    ParameterCount = baseMetadata.ParameterCount,
                    ParameterTypes = baseMetadata.ParameterTypes,
                    TestMethodParameterTypes = baseMetadata.TestMethodParameterTypes,
                    Hooks = baseMetadata.Hooks,
                    FilePath = baseMetadata.FilePath,
                    LineNumber = baseMetadata.LineNumber,
                    MethodMetadata = baseMetadata.MethodMetadata,
                    GenericTypeInfo = baseMetadata.GenericTypeInfo,
                    GenericMethodInfo = baseMetadata.GenericMethodInfo,
                    GenericMethodTypeArguments = baseMetadata.GenericMethodTypeArguments,
                    AttributeFactory = baseMetadata.AttributeFactory,
                    PropertyInjections = baseMetadata.PropertyInjections
                };

                expandedMetadata.Add(expandedTest);

                // Limit expansion to prevent memory issues with very large data sets
                if (combinationIndex >= 10000)
                {
                    Console.WriteLine($"Warning: Test {testName} has more than 10000 data combinations. Limiting to 10000 for discovery.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to expand data sources for {testName}: {ex.Message}. Falling back to single test.");
            return
            [
                baseMetadata
            ];
        }

        // If no combinations were generated, return the base metadata
        if (expandedMetadata.Count == 0)
        {
            return
            [
                baseMetadata
            ];
        }

        return expandedMetadata;
    }

    // Removed CountDataSourceItems - no longer needed since we work directly with IDataSourceAttribute

    private static int ExtractRepeatCount(Type testClass, MethodInfo testMethod)
    {
        // Check method level first
        var methodRepeat = testMethod.GetCustomAttribute<RepeatAttribute>();
        if (methodRepeat != null)
        {
            return methodRepeat.Times;
        }

        // Check class level
        var classRepeat = testClass.GetCustomAttribute<RepeatAttribute>();
        if (classRepeat != null)
        {
            return classRepeat.Times;
        }

        // Check assembly level
        var assemblyRepeat = testClass.Assembly.GetCustomAttribute<RepeatAttribute>();
        if (assemblyRepeat != null)
        {
            return assemblyRepeat.Times;
        }

        return 1;
    }

    private static string GenerateDataDrivenTestName(Type testClass, MethodInfo testMethod, int methodIndex, int classIndex, int repeatIndex, int testIndex)
    {
        return $"{testClass.Name}.{testMethod.Name}[{testIndex}]";
    }

    private static TestMetadata BuildSingleTestMetadata(Type testClass, MethodInfo testMethod, string testName, int repeatIndex)
    {
        return BuildTestMetadata(testClass, testMethod, testName);
    }

    private static TestMetadata BuildTestMetadata(Type testClass, MethodInfo testMethod, string? testName = null)
    {
        if (testName == null)
        {
            testName = GenerateTestName(testClass, testMethod);
        }

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
        // Check for DisplayNameAttribute and extract the template
        var displayNameAttr = testMethod.GetCustomAttribute<DisplayNameAttribute>();
        if (displayNameAttr != null)
        {
            // Extract the display name template from the attribute
            // We can't fully process it here because we don't have parameter values yet
            // But we can at least show the template for tests without parameters
            var displayNameField = typeof(DisplayNameAttribute).GetField("displayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (displayNameField != null)
            {
                var displayNameValue = displayNameField.GetValue(displayNameAttr) as string;
                if (!string.IsNullOrEmpty(displayNameValue) && !displayNameValue!.Contains("$"))
                {
                    // If the display name doesn't have parameter placeholders, use it directly
                    return displayNameValue;
                }
            }
        }
        
        // Default format
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

    private static IDataSourceAttribute[] ExtractMethodDataSources(MethodInfo testMethod)
    {
        var dataSources = new List<IDataSourceAttribute>();

        // Simply check for IDataSourceAttribute interface
        foreach (var attr in testMethod.GetCustomAttributes())
        {
            if (attr is IDataSourceAttribute dataSourceAttr)
            {
                dataSources.Add(dataSourceAttr);
            }
        }

        return dataSources.ToArray();
    }

    private static IDataSourceAttribute[] ExtractClassDataSources(Type testClass)
    {
        var dataSources = new List<IDataSourceAttribute>();


        // Simply check for IDataSourceAttribute interface
        foreach (var attr in testClass.GetCustomAttributes())
        {
            if (attr is IDataSourceAttribute dataSourceAttr)
            {
                dataSources.Add(dataSourceAttr);
            }
        }

        return dataSources.ToArray();
    }

    // Removed - no longer needed since we work directly with IDataSourceAttribute

    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicProperties' in call to 'System.Type.GetProperties(BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    private static PropertyDataSource[] ExtractPropertyDataSources([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass)
    {
        var propertyDataSources = new List<PropertyDataSource>();

        var properties = testClass.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            // Simply check for IDataSourceAttribute interface
            foreach (var attr in property.GetCustomAttributes())
            {
                if (attr is IDataSourceAttribute dataSourceAttr)
                {
                    propertyDataSources.Add(new PropertyDataSource
                    {
                        PropertyName = property.Name,
                        PropertyType = property.PropertyType,
                        DataSource = dataSourceAttr
                    });
                }
            }
        }

        return propertyDataSources.ToArray();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.NonPublicMethods' in call to 'System.Type.GetMethod(String, BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods', 'DynamicallyAccessedMemberTypes.NonPublicMethods' in call to 'System.Type.GetMethod(String, BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2067:'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'System.Activator.CreateInstance(Type)'", Justification = "Reflection mode requires dynamic access")]
    // Removed CreateMethodDataSource and CreateClassDataSource - no longer needed since we work directly with IDataSourceAttribute

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


    // Removed CreateDataSourceFromGenerator - no longer needed since we work directly with IDataSourceAttribute

    private static async IAsyncEnumerable<object?[]> CreateAsyncGeneratorEnumerable(
        Attribute attr,
        Type attrType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass,
        MethodInfo testMethod,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var metadata = CreateDataGeneratorMetadata(testClass, testMethod);

        // Get the GenerateDataSourcesAsync method
        var generateMethod = attrType.GetMethod("GenerateDataSourcesAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (generateMethod == null)
        {
            throw new InvalidOperationException($"Could not find GenerateDataSourcesAsync method on {attrType.Name}");
        }

        // Invoke the async generator
        var result = generateMethod.Invoke(attr, [metadata]);

        // Handle the async enumerable result
        if (result is IAsyncEnumerable<object> asyncEnumerable)
        {
            await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken))
            {
                // Use async-aware processing for discovery
                var dataRows = AsyncDataSourceHelper.ProcessAsyncGeneratorItemsForDiscovery(item);
                foreach (var dataRow in dataRows)
                {
                    yield return dataRow;
                }
            }
        }
    }

    private static DataGeneratorMetadata CreateDataGeneratorMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass, MethodInfo testMethod)
    {
        // Create TypeReference for the class
        var classTypeRef = new TypeReference
        {
            AssemblyQualifiedName = testClass.AssemblyQualifiedName
        };

        // Create AssemblyMetadata
        var assemblyMetadata = AssemblyMetadata.GetOrAdd(testClass.Assembly.FullName ?? testClass.Assembly.GetName().Name ?? "Unknown",
            () => new AssemblyMetadata { Name = testClass.Assembly.FullName ?? testClass.Assembly.GetName().Name ?? "Unknown" });

        // Create ClassMetadata
        var classMetadata = ClassMetadata.GetOrAdd(testClass.FullName ?? testClass.Name, () => new ClassMetadata
        {
            Name = testClass.Name,
            TypeReference = classTypeRef,
            Type = testClass,
            Namespace = testClass.Namespace,
            Assembly = assemblyMetadata,
            Parameters = [
            ], // Constructor parameters, empty for now
            Properties = [
            ], // Properties, empty for now
            Parent = null // Parent class, null for now
        });

        // Create parameter metadata for the test method
        var parameterMetadataList = new List<ParameterMetadata>();
        var methodParams = testMethod.GetParameters();
        foreach (var param in methodParams)
        {
            var paramTypeRef = new TypeReference
            {
                AssemblyQualifiedName = param.ParameterType.AssemblyQualifiedName
            };

            var paramMetadata = new ParameterMetadata(param.ParameterType)
            {
                Name = param.Name ?? "param",
                TypeReference = paramTypeRef,
                ReflectionInfo = param  // This is crucial for MatrixDataSourceAttribute
            };

            parameterMetadataList.Add(paramMetadata);
        }

        // Create MethodMetadata with actual method info
        var methodMetadata = new MethodMetadata
        {
            Name = testMethod.Name,
            TypeReference = classTypeRef,
            Type = testClass,
            Parameters = parameterMetadataList.ToArray(),
            GenericTypeCount = testMethod.IsGenericMethodDefinition ? testMethod.GetGenericArguments().Length : 0,
            Class = classMetadata,
            ReturnTypeReference = new TypeReference { AssemblyQualifiedName = testMethod.ReturnType.AssemblyQualifiedName },
            ReturnType = testMethod.ReturnType
        };

        return new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
            MembersToGenerate = parameterMetadataList.ToArray(),  // Pass the parameters as members to generate
            TestInformation = methodMetadata,
            Type = global::TUnit.Core.Enums.DataGeneratorType.TestParameters,
            TestSessionId = Guid.NewGuid().ToString(),
            TestClassInstance = null,
            ClassInstanceArguments = null
        };
    }

    private static List<object?[]> ProcessGeneratorResult(object? result)
    {
        var items = new List<object?[]>();

        if (result is IEnumerable<object> enumerable)
        {
            foreach (var item in enumerable)
            {
                // Use async-aware processing for better handling of async data sources
                items.AddRange(AsyncDataSourceHelper.ProcessAsyncGeneratorItemsForDiscovery(item));
            }
        }

        return items;
    }

    private static List<object?[]> ProcessGeneratorItem(object? item)
    {
        var items = new List<object?[]>();

        if (item is Func<Task<object?[]?>> taskFunc)
        {
            var data = taskFunc().GetAwaiter().GetResult();
            if (data != null)
            {
                items.Add(data);
            }
        }
        else if (item is Func<Task<object?>> singleTaskFunc)
        {
            var data = singleTaskFunc().GetAwaiter().GetResult();
            items.Add([data]);
        }
        else if (item is Task<object?[]?> task)
        {
            var data = task.GetAwaiter().GetResult();
            if (data != null)
            {
                items.Add(data);
            }
        }
        else if (item is Func<object?[]?> func)
        {
            var data = func();
            if (data != null)
            {
                items.Add(data);
            }
        }
        else if (item is Func<object?> singleFunc)
        {
            var data = singleFunc();
            items.Add([data]);
        }
        else if (item is object?[] array)
        {
            items.Add(array);
        }
        else if (global::TUnit.Engine.Helpers.TupleHelper.TryParseTupleToObjectArray(item, out object?[]? tupleValues))
        {
            items.Add(tupleValues!);
        }
        else if (item != null)
        {
            // Check if it's a Func<Task<T>> where T is a specific type
            var itemType = item.GetType();
            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Func<>))
            {
                var returnType = itemType.GetGenericArguments()[0];
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    // It's a Func<Task<T>>, invoke it and await the result
                    var funcDelegate = (Delegate)item;
                    var taskResult = funcDelegate.DynamicInvoke();
                    if (taskResult is Task taskObj)
                    {
                        taskObj.Wait();
                        var resultProperty = taskObj.GetType().GetProperty("Result");
                        if (resultProperty != null)
                        {
                            var result = resultProperty.GetValue(taskObj);
                            items.Add([result]);
                        }
                    }
                }
                else
                {
                    // It's a regular Func<T>, invoke it
                    var regularFunc = (Delegate)item;
                    var result = regularFunc.DynamicInvoke();
                    items.Add([result]);
                }
            }
            else
            {
                items.Add([item]);
            }
        }

        return items;
    }

    private static Func<CancellationToken, IAsyncEnumerable<object?[]>> CreateAsyncDataSourceFactory(MethodInfo method, object? instance, object?[] args)
    {
        return cancellationToken =>
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

            // If result is null, return empty
            if (result == null)
            {
                return [];
            }

            var resultType = result.GetType();

            // If result is a tuple, extract values
            if (resultType.Name.StartsWith("ValueTuple"))
            {
                // Extract tuple values
                var fields = resultType.GetFields();
                var values = fields.Select(f => f.GetValue(result)).ToArray();
                return [[values]];
            }

            // If result is an array of tuples, extract each tuple's values
            if (resultType.IsArray && resultType.GetElementType()?.Name.StartsWith("ValueTuple") == true)
            {
                var array = (Array)result;
                var results = new List<object?[]>();
                foreach (var item in array)
                {
                    if (item != null)
                    {
                        var tupleType = item.GetType();
                        var fields = tupleType.GetFields();
                        var values = fields.Select(f => f.GetValue(item)).ToArray();
                        results.Add(values);
                    }
                }
                return results;
            }

            // If result is an array or IEnumerable (but not string), treat each element as separate test data
            if (result is System.Collections.IEnumerable enumerable and not string)
            {
                var results = new List<object?[]>();
                foreach (var item in enumerable)
                {
                    results.Add([item]);
                }
                return results;
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
        try
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
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to compile instance factory for {ctor.DeclaringType?.Name}: {ex.Message}");
            // Return a fallback factory that uses reflection
            return args =>
            {
                try
                {
                    return Activator.CreateInstance(ctor.DeclaringType!, args)
                           ?? throw new InvalidOperationException("Failed to create instance");
                }
                catch (Exception invokeEx)
                {
                    throw new InvalidOperationException($"Failed to create instance of {ctor.DeclaringType?.Name}", invokeEx);
                }
            };
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Using member 'System.Linq.Expressions.Expression.Call(Type, String, Type[], params Expression[])' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling", Justification = "Reflection mode cannot support AOT")]
    private static Func<object, object?[], Task> CreateTestInvoker(Type testClass, MethodInfo testMethod)
    {
        return _expressionCache.GetOrCreateTestInvoker(testClass, testMethod,
            key => CompileTestInvoker(key.Item1, key.Item2));
    }

    private static Func<object, object?[], Task> CompileTestInvoker(Type testClass, MethodInfo testMethod)
    {
        try
        {
            // Skip compilation for generic methods - they can't be compiled and will use reflection
            if (testMethod.IsGenericMethodDefinition || testMethod.ContainsGenericParameters)
            {
                // Return the reflection-based fallback directly without warning
                return (instance, args) =>
                {
                    try
                    {
                        var result = testMethod.Invoke(instance, args);
                        if (result is Task task)
                        {
                            return task;
                        }
                        return Task.CompletedTask;
                    }
                    catch (TargetInvocationException tie)
                    {
                        ExceptionDispatchInfo.Capture(tie.InnerException ?? tie).Throw();
                        throw;
                    }
                };
            }

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var argsParam = Expression.Parameter(typeof(object[]), "args");

            var instanceExpr = testMethod.IsStatic
                ? null
                : Expression.Convert(instanceParam, testClass);

            var parameters = testMethod.GetParameters();
            var argExpressions = new Expression[parameters.Length];

            // Count required parameters (non-optional)
            var requiredParamCount = parameters.Count(p => !p.IsOptional);

            // First, add a runtime check for parameter count mismatch
            var paramCountMismatchCheck = Expression.IfThen(
                Expression.OrElse(
                    Expression.LessThan(
                        Expression.ArrayLength(argsParam),
                        Expression.Constant(requiredParamCount)
                    ),
                    Expression.GreaterThan(
                        Expression.ArrayLength(argsParam),
                        Expression.Constant(parameters.Length)
                    )
                ),
                Expression.Throw(
                    Expression.New(
                        typeof(ArgumentException).GetConstructor([typeof(string)])!,
                        Expression.Call(
                            typeof(string).GetMethod("Format", [typeof(string), typeof(object[])])!,
                            Expression.Constant($"Test method '{testClass.Name}.{testMethod.Name}' expects {{0}}-{{1}} parameter(s) but received {{2}}. " +
                                              $"Expected parameters: {string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}{(p.IsOptional ? " (optional)" : "")}"))}"                          ),
                            Expression.NewArrayInit(
                                typeof(object),
                                Expression.Convert(Expression.Constant(requiredParamCount), typeof(object)),
                                Expression.Convert(Expression.Constant(parameters.Length), typeof(object)),
                                Expression.Convert(Expression.ArrayLength(argsParam), typeof(object))
                            )
                        )
                    )
                )
            );

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var argIndex = i;

                // Check if this parameter has a value in the args array
                var hasValue = Expression.LessThan(
                    Expression.Constant(argIndex),
                    Expression.ArrayLength(argsParam)
                );

                if (parameter.IsOptional)
                {
                    // For optional parameters, use default value if not provided
                    var indexExpr = Expression.ArrayIndex(argsParam, Expression.Constant(i));

                    // Use CastHelper.Cast to handle implicit/explicit conversion operators
                    var castMethod = typeof(CastHelper).GetMethod(nameof(CastHelper.Cast),
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        [typeof(Type), typeof(object)],
                        null);

                    var castExpr = Expression.Call(
                        castMethod!,
                        Expression.Constant(parameter.ParameterType),
                        indexExpr
                    );

                    // Convert the result to the expected parameter type
                    var convertedExpr = Expression.Convert(castExpr, parameter.ParameterType);

                    // Use conditional to check if we have the argument or use default
                    argExpressions[i] = Expression.Condition(
                        hasValue,
                        convertedExpr,
                        Expression.Constant(parameter.DefaultValue, parameter.ParameterType)
                    );
                }
                else
                {
                    // For required parameters, always use the provided value
                    var indexExpr = Expression.ArrayIndex(argsParam, Expression.Constant(i));

                    // Use CastHelper.Cast to handle implicit/explicit conversion operators
                    var castMethod = typeof(CastHelper).GetMethod(nameof(CastHelper.Cast),
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        [typeof(Type), typeof(object)],
                        null);

                    var castExpr = Expression.Call(
                        castMethod!,
                        Expression.Constant(parameter.ParameterType),
                        indexExpr
                    );

                    // Convert the result to the expected parameter type
                    argExpressions[i] = Expression.Convert(castExpr, parameter.ParameterType);
                }
            }

            var callExpr = testMethod.IsStatic
                ? Expression.Call(testMethod, argExpressions)
                : Expression.Call(instanceExpr!, testMethod, argExpressions);

            Expression body;
            if (testMethod.ReturnType == typeof(Task))
            {
                body = Expression.Block(
                    paramCountMismatchCheck,
                    callExpr
                );
            }
            else if (testMethod.ReturnType == typeof(void))
            {
                body = Expression.Block(
                    paramCountMismatchCheck,
                    callExpr,
                    Expression.Constant(Task.CompletedTask)
                );
            }
            else if (testMethod.ReturnType.IsGenericType &&
                     testMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var callWithCheck = Expression.Block(
                    paramCountMismatchCheck,
                    callExpr
                );
                var taskType = testMethod.ReturnType.GetGenericArguments()[0];
                var convertMethod = typeof(ReflectionTestDataCollector)
                    .GetMethod(nameof(ConvertToNonGenericTask), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(taskType);
                body = Expression.Call(convertMethod, callWithCheck);
            }
            else if (testMethod.ReturnType == typeof(ValueTask))
            {
                var callWithCheck = Expression.Block(
                    paramCountMismatchCheck,
                    callExpr
                );
                body = Expression.Call(
                    callWithCheck,
                    typeof(ValueTask).GetMethod("AsTask")!);
            }
            else if (testMethod.ReturnType.IsGenericType &&
                     testMethod.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var callWithCheck = Expression.Block(
                    paramCountMismatchCheck,
                    callExpr
                );
                var taskType = testMethod.ReturnType.GetGenericArguments()[0];
                var convertMethod = typeof(ReflectionTestDataCollector)
                    .GetMethod(nameof(ConvertValueTaskToTask), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(taskType);
                body = Expression.Call(convertMethod, callWithCheck);
            }
            else
            {
                // Sync method returning a value
                body = Expression.Block(
                    paramCountMismatchCheck,
                    callExpr,
                    Expression.Constant(Task.CompletedTask)
                );
            }

            var lambda = Expression.Lambda<Func<object, object?[], Task>>(
                body,
                instanceParam,
                argsParam);

            return lambda.Compile();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to compile test invoker for {testClass.Name}.{testMethod.Name}: {ex.Message}");
            // Return a fallback invoker that uses reflection
            return (instance, args) =>
            {
                try
                {
                    var result = testMethod.Invoke(instance, args);
                    if (result is Task task)
                    {
                        return task;
                    }
                    return Task.CompletedTask;
                }
                catch (Exception invokeEx)
                {
                    return Task.FromException(invokeEx);
                }
            };
        }
    }

    private static Task ConvertToNonGenericTask<T>(Task<T> task)
    {
        return task;
    }

    private static Task ConvertValueTaskToTask<T>(ValueTask<T> valueTask)
    {
        return valueTask.AsTask();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Using member 'System.Reflection.Assembly.GetTypes()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code", Justification = "Reflection mode cannot support trimming")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
    private static TestHooks DiscoverHooks([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type testClass)
    {
        var beforeClass = new List<HookMetadata>();
        var afterClass = new List<HookMetadata>();
        var beforeTest = new List<HookMetadata>();
        var afterTest = new List<HookMetadata>();

        // Only scan the test class hierarchy, not all assemblies
        var currentType = testClass;
        while (currentType != null && currentType != typeof(object))
        {
            try
            {
                var methods = currentType.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.DeclaredOnly);

                foreach (var method in methods)
                {
                    // Check for Before attributes
                    var beforeAttrs = method.GetCustomAttributes<BeforeAttribute>();
                    foreach (var attr in beforeAttrs)
                    {
                        // Skip TestDiscovery hooks - they run during discovery phase, not test execution
                        if (attr.HookType.HasFlag(HookType.TestDiscovery))
                            continue;

                        var hookMetadata = CreateHookMetadata(method, attr, true);
                        if (hookMetadata != null)
                        {
                            if (attr.HookType.HasFlag(HookType.Class))
                                beforeClass.Add(hookMetadata);
                            if (attr.HookType.HasFlag(HookType.Test))
                                beforeTest.Add(hookMetadata);
                        }
                    }

                    // Check for After attributes
                    var afterAttrs = method.GetCustomAttributes<AfterAttribute>();
                    foreach (var attr in afterAttrs)
                    {
                        // Skip TestDiscovery hooks - they run during discovery phase, not test execution
                        if (attr.HookType.HasFlag(HookType.TestDiscovery))
                            continue;

                        var hookMetadata = CreateHookMetadata(method, attr, false);
                        if (hookMetadata != null)
                        {
                            if (attr.HookType.HasFlag(HookType.Class))
                                afterClass.Add(hookMetadata);
                            if (attr.HookType.HasFlag(HookType.Test))
                                afterTest.Add(hookMetadata);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to scan hooks in type {currentType.FullName}: {ex.Message}");
            }

            currentType = currentType.BaseType;
        }

        // Also check for assembly-level hooks in the test class's assembly only
        try
        {
            var assembly = testClass.Assembly;
            var assemblyTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && ShouldScanTypeForHooks(t))
                .ToList();

            foreach (var type in assemblyTypes)
            {
                try
                {
                    var methods = type.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static);

                    foreach (var method in methods)
                    {
                        // Check for assembly-level Before hooks
                        var beforeAttrs = method.GetCustomAttributes<BeforeAttribute>()
                            .Where(a => a.HookType.HasFlag(HookType.Assembly) && !a.HookType.HasFlag(HookType.TestDiscovery));
                        foreach (var attr in beforeAttrs)
                        {
                            var hookMetadata = CreateHookMetadata(method, attr, true);
                            if (hookMetadata != null)
                            {
                                beforeClass.Add(hookMetadata);
                            }
                        }

                        // Check for assembly-level After hooks
                        var afterAttrs = method.GetCustomAttributes<AfterAttribute>()
                            .Where(a => a.HookType.HasFlag(HookType.Assembly) && !a.HookType.HasFlag(HookType.TestDiscovery));
                        foreach (var attr in afterAttrs)
                        {
                            var hookMetadata = CreateHookMetadata(method, attr, false);
                            if (hookMetadata != null)
                            {
                                afterClass.Add(hookMetadata);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip hooks from this type and continue
                    continue;
                }
            }
        }
        catch (Exception)
        {
            // Return empty hooks if assembly scan fails
            // This allows tests to run without hooks rather than failing entirely
        }

        return new TestHooks
        {
            BeforeClass = beforeClass.ToArray(),
            AfterClass = afterClass.ToArray(),
            BeforeTest = beforeTest.ToArray(),
            AfterTest = afterTest.ToArray()
        };
    }

    private static bool ShouldScanTypeForHooks(Type type)
    {
        // Skip types that are likely to cause issues
        var name = type.FullName ?? type.Name;

        if (name.Contains("PrivateImplementationDetails") ||
            name.Contains("__") ||
            name.Contains("<>") ||
            name.Contains("Resources") ||
            name.Contains("AssemblyInfo"))
        {
            return false;
        }

        return true;
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
        var hookInvoker = CreateHookInvoker(method);
        if (hookInvoker == null)
        {
            return null;
        }

        var level = HookLevel.Test;
        if (hookAttr.HookType.HasFlag(HookType.Assembly))
        {
            level = HookLevel.Assembly;
        }
        else if (hookAttr.HookType.HasFlag(HookType.Class))
        {
            level = HookLevel.Class;
        }

        return new HookMetadata
        {
            Name = method.Name,
            Level = level,
            Order = hookAttr.Order,
            DeclaringType = method.DeclaringType,
            IsStatic = method.IsStatic,
            IsAsync = IsAsyncMethod(method),
            ReturnsValueTask = method.ReturnType == typeof(ValueTask),
            HookInvoker = hookInvoker
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Using member 'System.Linq.Expressions.Expression.Property(Expression, String)' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code", Justification = "Reflection mode cannot support trimming")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Using member 'System.Linq.Expressions.Expression.Lambda' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling", Justification = "Reflection mode cannot support AOT")]
    private static Func<object, TestContext, Task>? CreateHookInvoker(MethodInfo method)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var contextParam = Expression.Parameter(typeof(TestContext), "context");

        // Check parameters
        var parameters = method.GetParameters();
        if (parameters.Length > 2)
            {
                throw new InvalidOperationException(
                    $"Hook method {method.Name} has too many parameters. Maximum allowed is 2, but found {parameters.Length}.");
            }

            Expression? callExpr;
            if (parameters.Length == 0)
            {
                // No parameters
                callExpr = method.IsStatic
                    ? Expression.Call(method)
                    : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method);
            }
            else if (parameters.Length == 1)
            {
                // Single parameter
                var paramType = parameters[0].ParameterType;

                if (paramType == typeof(TestContext))
                {
                    callExpr = method.IsStatic
                        ? Expression.Call(method, contextParam)
                        : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method, contextParam);
                }
                else if (paramType == typeof(ClassHookContext))
                {
                    var classContextExpr = Expression.Property(contextParam, nameof(TestContext.ClassContext));
                    callExpr = method.IsStatic
                        ? Expression.Call(method, classContextExpr)
                        : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method, classContextExpr);
                }
                else if (paramType == typeof(AssemblyHookContext))
                {
                    var classContextExpr = Expression.Property(contextParam, nameof(TestContext.ClassContext));
                    var assemblyContextExpr = Expression.Property(classContextExpr, nameof(ClassHookContext.AssemblyContext));
                    callExpr = method.IsStatic
                        ? Expression.Call(method, assemblyContextExpr)
                        : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method, assemblyContextExpr);
                }
                else if (paramType == typeof(TestSessionContext))
                {
                    var classContextExpr = Expression.Property(contextParam, nameof(TestContext.ClassContext));
                    var assemblyContextExpr = Expression.Property(classContextExpr, nameof(ClassHookContext.AssemblyContext));
                    var sessionContextExpr = Expression.Property(assemblyContextExpr, nameof(AssemblyHookContext.TestSessionContext));
                    callExpr = method.IsStatic
                        ? Expression.Call(method, sessionContextExpr)
                        : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method, sessionContextExpr);
                }
                else if (paramType == typeof(CancellationToken))
                {
                    var cancellationTokenExpr = Expression.Property(contextParam, nameof(TestContext.CancellationToken));
                    callExpr = method.IsStatic
                        ? Expression.Call(method, cancellationTokenExpr)
                        : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method, cancellationTokenExpr);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Hook method {method.Name} has unsupported parameter type {paramType}. " +
                        "Supported types are: TestContext, ClassHookContext, AssemblyHookContext, TestSessionContext, CancellationToken.");
                }
            }
            else if (parameters.Length == 2)
            {
                // Two parameters - typically context + CancellationToken
                var argsList = new List<Expression>();

                for (int i = 0; i < 2; i++)
                {
                    var paramType = parameters[i].ParameterType;

                    if (paramType == typeof(TestContext))
                    {
                        argsList.Add(contextParam);
                    }
                    else if (paramType == typeof(ClassHookContext))
                    {
                        var classContextExpr = Expression.Property(contextParam, nameof(TestContext.ClassContext));
                        argsList.Add(classContextExpr);
                    }
                    else if (paramType == typeof(AssemblyHookContext))
                    {
                        var classContextExpr = Expression.Property(contextParam, nameof(TestContext.ClassContext));
                        var assemblyContextExpr = Expression.Property(classContextExpr, nameof(ClassHookContext.AssemblyContext));
                        argsList.Add(assemblyContextExpr);
                    }
                    else if (paramType == typeof(TestSessionContext))
                    {
                        var classContextExpr = Expression.Property(contextParam, nameof(TestContext.ClassContext));
                        var assemblyContextExpr = Expression.Property(classContextExpr, nameof(ClassHookContext.AssemblyContext));
                        var sessionContextExpr = Expression.Property(assemblyContextExpr, nameof(AssemblyHookContext.TestSessionContext));
                        argsList.Add(sessionContextExpr);
                    }
                    else if (paramType == typeof(CancellationToken))
                    {
                        argsList.Add(Expression.Property(contextParam, nameof(TestContext.CancellationToken)));
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Hook method {method.Name} has unsupported parameter type {paramType} at position {i}. " +
                            "Supported types are: TestContext, ClassHookContext, AssemblyHookContext, TestSessionContext, CancellationToken.");
                    }
                }

                // Use correct overload for static vs instance methods
                if (method.IsStatic)
                {
                    // For static methods, use overload without instance
                    callExpr = Expression.Call(method, argsList);
                }
                else
                {
                    // For instance methods, include the instance
                    callExpr = Expression.Call(Expression.Convert(instanceParam, method.DeclaringType!), method, argsList);
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Hook method {method.Name} has unexpected parameter count {parameters.Length}. " +
                    "Hook methods must have 0, 1, or 2 parameters.");
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
                throw new InvalidOperationException(
                    $"Hook method {method.Name} has unsupported return type {method.ReturnType}. " +
                    "Hook methods must return void, Task, or ValueTask.");
            }

        var lambda = Expression.Lambda<Func<object, TestContext, Task>>(
            body,
            instanceParam,
            contextParam);

        return lambda.Compile();
    }

    private static bool IsAsyncMethod(MethodInfo method)
    {
        return method.ReturnType == typeof(Task) ||
               method.ReturnType == typeof(ValueTask) ||
               (method.ReturnType.IsGenericType &&
                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
    }

    private static bool IsCompilerGenerated(Type type)
    {
        // Check if type has CompilerGeneratedAttribute
        if (type.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
        {
            return true;
        }

        // Check for compiler-generated naming patterns
        var typeName = type.Name;

        // Compiler-generated types often start with <> or contain special characters
        if (typeName.StartsWith("<>") || typeName.StartsWith("<"))
        {
            return true;
        }

        // Check for async state machine pattern (e.g., <MethodName>d__1)
        if (typeName.Contains(">d__"))
        {
            return true;
        }

        // Check for display class pattern (e.g., <>c__DisplayClass)
        if (typeName.Contains("__DisplayClass"))
        {
            return true;
        }

        // Check for anonymous type pattern
        if (typeName.Contains("AnonymousType"))
        {
            return true;
        }

        return false;
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
            Parameters = [
            ],
            Properties = [
            ]
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

    private static TestMetadata CreateFailedTestMetadataForAssembly(Assembly assembly, Exception ex)
    {
        var testName = $"[ASSEMBLY SCAN FAILED] {assembly.GetName().Name}";
        var testClass = typeof(ReflectionTestDataCollector);
        var displayName = $"{testName} - {ex.Message}";

        // Create a special metadata that will yield a failed data combination
        return new FailedTestMetadata(ex, displayName)
        {
            TestName = testName,
            TestClassType = testClass,
            TestMethodName = "AssemblyScanFailed",
            MethodMetadata = CreateDummyMethodMetadata(testClass, "AssemblyScanFailed"),
            AttributeFactory = () => [
            ]
        };
    }

    private static TestMetadata CreateFailedTestMetadata(Type type, MethodInfo method, Exception ex)
    {
        var testName = $"[DISCOVERY FAILED] {type.FullName}.{method.Name}";
        var displayName = $"{testName} - {ex.Message}";

        // Create a special metadata that will yield a failed data combination
        return new FailedTestMetadata(ex, displayName)
        {
            TestName = testName,
            TestClassType = type,
            TestMethodName = method.Name,
            MethodMetadata = CreateMethodMetadata(type, method),
            AttributeFactory = () => method.GetCustomAttributes().ToArray()
        };
    }

    // Dummy method to use as a placeholder for failed tests
    private static void DummyFailedTestMethod() { }

    private static MethodMetadata CreateMethodMetadata(Type type, MethodInfo method)
    {
        return new MethodMetadata
        {
            Name = method.Name,
            Type = type,
            Class = new ClassMetadata
            {
                Name = type.Name,
                Type = type,
                TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName!),
                Namespace = type.Namespace ?? string.Empty,
                Assembly = new AssemblyMetadata { Name = type.Assembly.GetName().Name ?? "Unknown" },
                Parameters = [
                ],
                Properties = [
                ],
                Parent = null
            },
            Parameters = method.GetParameters().Select(p => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? "unnamed",
                TypeReference = TypeReference.CreateConcrete(p.ParameterType.AssemblyQualifiedName!),
                ReflectionInfo = p
            }).ToArray(),
            GenericTypeCount = method.IsGenericMethodDefinition ? method.GetGenericArguments().Length : 0,
            ReturnTypeReference = TypeReference.CreateConcrete(method.ReturnType.AssemblyQualifiedName!),
            ReturnType = method.ReturnType,
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName!)
        };
    }

    private static MethodMetadata CreateDummyMethodMetadata(Type type, string methodName)
    {
        return new MethodMetadata
        {
            Name = methodName,
            Type = type,
            Class = new ClassMetadata
            {
                Name = type.Name,
                Type = type,
                TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName!),
                Namespace = type.Namespace ?? string.Empty,
                Assembly = new AssemblyMetadata { Name = type.Assembly.GetName().Name ?? "Unknown" },
                Parameters = [
                ],
                Properties = [
                ],
                Parent = null
            },
            Parameters = [
            ],
            GenericTypeCount = 0,
            ReturnTypeReference = TypeReference.CreateConcrete(typeof(void).AssemblyQualifiedName!),
            ReturnType = typeof(void),
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName!)
        };
    }

    // Special test metadata class for tests that failed during discovery
    private sealed class FailedTestMetadata : TestMetadata
    {
        private readonly Exception _exception;
        private readonly string _displayName;

        public FailedTestMetadata(Exception exception, string displayName)
        {
            _exception = exception;
            _displayName = displayName;
        }

        public override Func<IAsyncEnumerable<TestDataCombination>> DataCombinationGenerator
        {
            get => () => GenerateFailedCombination();
        }

        public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
        {
            get => (context, metadata) => new FailedExecutableTest(_exception)
            {
                TestId = context.TestId,
                DisplayName = context.DisplayName,
                Metadata = metadata,
                Arguments = context.Arguments,
                ClassArguments = context.ClassArguments,
                PropertyValues = context.PropertyValues,
                BeforeTestHooks = context.BeforeTestHooks,
                AfterTestHooks = context.AfterTestHooks,
                Context = context.Context
            };
        }

        private async IAsyncEnumerable<TestDataCombination> GenerateFailedCombination()
        {
            yield return new TestDataCombination
            {
                DataGenerationException = _exception,
                DisplayName = _displayName
            };
            await Task.CompletedTask;
        }
    }

    // Special test metadata class for expanded tests during discovery
    private sealed class ExpandedReflectionTestMetadata : TestMetadata
    {
        private readonly Type _testClass;
        private readonly MethodInfo _testMethod;
        private readonly TestDataCombination _combination;
        private readonly int _combinationIndex;
        private Func<IAsyncEnumerable<TestDataCombination>>? _dataCombinationGenerator;
        private Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest>? _createExecutableTestFactory;

        public ExpandedReflectionTestMetadata(
            Type testClass,
            MethodInfo testMethod,
            TestDataCombination combination,
            int combinationIndex)
        {
            _testClass = testClass;
            _testMethod = testMethod;
            _combination = combination;
            _combinationIndex = combinationIndex;
        }

        public override Func<IAsyncEnumerable<TestDataCombination>> DataCombinationGenerator
        {
            get
            {
                if (_dataCombinationGenerator == null)
                {
                    _dataCombinationGenerator = () => GenerateSingleCombination();
                }
                return _dataCombinationGenerator;
            }
        }

        public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
        {
            get
            {
                if (_createExecutableTestFactory == null)
                {
                    _createExecutableTestFactory = CreateExecutableTest;
                }
                return _createExecutableTestFactory;
            }
        }

        private async IAsyncEnumerable<TestDataCombination> GenerateSingleCombination()
        {
            // Return only the specific combination this test represents
            yield return _combination;
            await Task.CompletedTask;
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

                var instance = InstanceFactory(context.ClassArguments);

                // Apply property values using unified PropertyInjector
                await PropertyInjector.InjectPropertiesAsync(
                    instance,
                    context.PropertyValues,
                    metadata.PropertyInjections);

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

                    for (int i = 0; i < argsWithToken.Length; i++)
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
                PropertyValues = context.PropertyValues,
                BeforeTestHooks = context.BeforeTestHooks,
                AfterTestHooks = context.AfterTestHooks,
                Context = context.Context
            };
        }
    }
}
