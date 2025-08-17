using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Discovery;

/// Discovers tests at runtime using reflection with assembly scanning and caching
[RequiresUnreferencedCode("Reflection-based test discovery requires unreferenced code")]
[RequiresDynamicCode("Expression compilation requires dynamic code generation")]
[SuppressMessage("Trimming", "IL2077:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The source field does not have matching annotations.")]
public sealed class ReflectionTestDataCollector : ITestDataCollector
{
    private static readonly ConcurrentDictionary<Assembly, bool> _scannedAssemblies = new();
    private static readonly ConcurrentBag<TestMetadata> _discoveredTests = new();
    private static readonly Lock _resultsLock = new(); // Only for final results aggregation
    private static readonly ConcurrentDictionary<Assembly, Type[]> _assemblyTypesCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo[]> _typeMethodsCache = new();

    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        // Disable assembly loading event handler to prevent recursive issues
        // This was causing problems when assemblies were loaded during scanning

        // Optimize: Pre-filter and allocate array instead of LINQ ToList()
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var assembliesList = new List<Assembly>(allAssemblies.Length);
        foreach (var assembly in allAssemblies)
        {
            if (ShouldScanAssembly(assembly))
            {
                assembliesList.Add(assembly);
            }
        }
        var assemblies = assembliesList;


        // Use throttled parallel processing to prevent thread pool exhaustion
        // Limit to 2x processor count to avoid overwhelming the thread pool
        var maxConcurrency = Math.Min(assemblies.Count, Environment.ProcessorCount * 2);
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new Task<List<TestMetadata>>[assemblies.Count];

        for (var i = 0; i < assemblies.Count; i++)
        {
            var assembly = assemblies[i];
            var index = i;

            tasks[index] = Task.Run(async () =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    // Use lock-free ConcurrentDictionary for assembly tracking
                    if (!_scannedAssemblies.TryAdd(assembly, true))
                    {
                        return
                        [
                        ];
                    }

                    try
                    {
                        // Now we can properly await the async method
                        var testsInAssembly = await DiscoverTestsInAssembly(assembly).ConfigureAwait(false);
                        return testsInAssembly;
                    }
                    catch (Exception ex)
                    {
                        // Create a failed test metadata for the assembly that couldn't be scanned
                        var failedTest = CreateFailedTestMetadataForAssembly(assembly, ex);
                        return
                        [
                            failedTest
                        ];
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });
        }

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // Reassemble results in original order
        var newTests = new List<TestMetadata>();
        foreach (var tests in results)
        {
            newTests.AddRange(tests);
        }

        // Discover dynamic tests from DynamicTestBuilderAttribute methods
        var dynamicTests = await DiscoverDynamicTests(testSessionId).ConfigureAwait(false);
        newTests.AddRange(dynamicTests);

        // Add to concurrent collection without locking
        foreach (var test in newTests)
        {
            _discoveredTests.Add(test);
        }

        // Only lock when creating the final result list
        lock (_resultsLock)
        {
            return _discoveredTests.ToList();
        }
    }

    public async IAsyncEnumerable<TestMetadata> CollectTestsStreamingAsync(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get assemblies to scan
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var assemblies = new List<Assembly>(allAssemblies.Length);
        foreach (var assembly in allAssemblies)
        {
            if (ShouldScanAssembly(assembly))
            {
                assemblies.Add(assembly);
            }
        }


        // Stream tests from each assembly
        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use lock-free ConcurrentDictionary for assembly tracking
            if (!_scannedAssemblies.TryAdd(assembly, true))
            {
                continue;
            }

            // Stream tests from this assembly
            await foreach (var test in DiscoverTestsInAssemblyStreamingAsync(assembly, cancellationToken))
            {
                // Use lock-free ConcurrentBag
                _discoveredTests.Add(test);
                yield return test;
            }
        }

        // Stream dynamic tests
        await foreach (var dynamicTest in DiscoverDynamicTestsStreamingAsync(testSessionId, cancellationToken))
        {
            _discoveredTests.Add(dynamicTest);
            yield return dynamicTest;
        }
    }

    private static IEnumerable<MethodInfo> GetAllTestMethods(Type type)
    {
        return _typeMethodsCache.GetOrAdd(type, static t =>
        {
            var methods = new List<MethodInfo>();
            var currentType = t;

            while (currentType != null && currentType != typeof(object))
            {
                methods.AddRange(currentType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
                currentType = currentType.BaseType;
            }

            return methods.ToArray();
        });
    }

    private static readonly HashSet<string> ExcludedAssemblyNames =
    [
        "mscorlib",
        "System",
        "System.Core",
        "System.Runtime",
        "System.Private.CoreLib",
        "System.Collections",
        "System.Linq",
        "System.Threading",
        "System.Text.RegularExpressions",
        "System.Diagnostics.Debug",
        "System.Runtime.Extensions",
        "System.Collections.Concurrent",
        "System.Text.Json",
        "System.Memory",
        "System.Net.Http",
        "System.IO.FileSystem",
        "System.Console",
        "System.Diagnostics.Process",
        "System.ComponentModel.TypeConverter",
        "System.ComponentModel.Primitives",
        "System.ObjectModel",
        "System.Private.Uri",
        "System.Private.Xml",
        "netstandard",

        // Microsoft platform assemblies
        "Microsoft.CSharp",
        "Microsoft.Win32.Primitives",
        "Microsoft.Win32.Registry",
        "Microsoft.VisualBasic.Core",
        "Microsoft.VisualBasic",

        // TUnit framework assemblies (except test projects)
        "TUnit",
        "TUnit.Core",
        "TUnit.Engine",
        "TUnit.Assertions",

        // Test platform assemblies
        "testhost",
        "Microsoft.TestPlatform.CoreUtilities",
        "Microsoft.TestPlatform.CommunicationUtilities",
        "Microsoft.TestPlatform.CrossPlatEngine",
        "Microsoft.TestPlatform.Common",
        "Microsoft.TestPlatform.PlatformAbstractions",
        "Microsoft.Testing.Platform",

        // Common third-party assemblies
        "Newtonsoft.Json",
        "Castle.Core",
        "Moq",
        "xunit.core",
        "xunit.assert",
        "xunit.execution.desktop",
        "nunit.framework",
        "FluentAssertions",
        "AutoFixture",
        "FakeItEasy",
        "Shouldly",
        "NSubstitute",
        "Rhino.Mocks"
    ];

    private static bool ShouldScanAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name == null)
        {
            return false;
        }

        if (ExcludedAssemblyNames.Contains(name))
        {
            return false;
        }

        if (name.EndsWith(".resources") || name.EndsWith(".XmlSerializers"))
        {
            return false;
        }

        if (assembly.IsDynamic)
        {
            return false;
        }

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
            // In single-file mode, assembly.Location might throw - but we should still scan the assembly
            // Don't return false here, continue with other checks
        }

        if (!assembly.GetReferencedAssemblies().Any(a =>
                a.Name != null && (a.Name.StartsWith("TUnit") || a.Name == "TUnit")))
        {
            return false;
        }

        return true;
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Reflection mode cannot support trimming")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'",
        Justification = "Reflection mode requires dynamic access")]
    private static async Task<List<TestMetadata>> DiscoverTestsInAssembly(Assembly assembly)
    {
        var discoveredTests = new List<TestMetadata>();

        var types = _assemblyTypesCache.GetOrAdd(assembly, asm =>
        {
            try
            {
                // In single file mode, GetExportedTypes might miss some types
                // Use GetTypes() instead which gets all types including nested ones
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                // Some types might fail to load, but we can still use the ones that loaded successfully
                // Optimize: Manual filtering with ArrayPool for better memory efficiency
                var loadedTypes = rtle.Types;
                if (loadedTypes == null) return [];

                // Use ArrayPool for temporary storage to reduce allocations
                var tempArray = ArrayPool<Type>.Shared.Rent(loadedTypes.Length);
                try
                {
                    var validCount = 0;
                    foreach (var type in loadedTypes)
                    {
                        if (type != null)
                        {
                            tempArray[validCount++] = type;
                        }
                    }

                    var result = new Type[validCount];
                    Array.Copy(tempArray, result, validCount);
                    return result;
                }
                finally
                {
                    ArrayPool<Type>.Shared.Return(tempArray);
                }
            }
            catch (Exception)
            {
                return [];
            }
        });

        if (types.Length == 0)
        {
            return discoveredTests;
        }

        var filteredTypes = types.Where(t => t.IsClass && !IsCompilerGenerated(t));

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
                var genericTests = await DiscoverGenericTests(type).ConfigureAwait(false);
                discoveredTests.AddRange(genericTests);
                continue;
            }

            MethodInfo[] testMethods;
            try
            {
                // Check if this class inherits tests from base classes
                var inheritsTests = type.IsDefined(typeof(InheritsTestsAttribute), inherit: false);

                if (inheritsTests)
                {
                    // Get all methods including inherited ones
                    // Optimize: Manual filtering instead of LINQ Where().ToArray()
                    var allMethods = GetAllTestMethods(type);
                    var testMethodsList = new List<MethodInfo>();
                    foreach (var method in allMethods)
                    {
                        if (method.IsDefined(typeof(TestAttribute), inherit: false) && !method.IsAbstract)
                        {
                            testMethodsList.Add(method);
                        }
                    }
                    testMethods = testMethodsList.ToArray();
                }
                else
                {
                    // Only get declared methods
                    // Optimize: Manual filtering instead of LINQ Where().ToArray()
                    var declaredMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    var testMethodsList = new List<MethodInfo>(declaredMethods.Length);
                    foreach (var method in declaredMethods)
                    {
                        if (method.IsDefined(typeof(TestAttribute), inherit: false) && !method.IsAbstract)
                        {
                            testMethodsList.Add(method);
                        }
                    }
                    testMethods = testMethodsList.ToArray();
                }
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var method in testMethods)
            {
                try
                {
                    discoveredTests.Add(await BuildTestMetadata(type, method).ConfigureAwait(false));
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

    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Reflection mode cannot support trimming")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'",
        Justification = "Reflection mode requires dynamic access")]
    private static async IAsyncEnumerable<TestMetadata> DiscoverTestsInAssemblyStreamingAsync(
        Assembly assembly,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {

        var types = _assemblyTypesCache.GetOrAdd(assembly, asm =>
        {
            try
            {
                // In single file mode, GetExportedTypes might miss some types
                // Use GetTypes() instead which gets all types including nested ones
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                // Some types might fail to load, but we can still use the ones that loaded successfully
                // Optimize: Manual filtering with ArrayPool for better memory efficiency
                var loadedTypes = rtle.Types;
                if (loadedTypes == null) return [];

                // Use ArrayPool for temporary storage to reduce allocations
                var tempArray = ArrayPool<Type>.Shared.Rent(loadedTypes.Length);
                try
                {
                    var validCount = 0;
                    foreach (var type in loadedTypes)
                    {
                        if (type != null)
                        {
                            tempArray[validCount++] = type;
                        }
                    }

                    var result = new Type[validCount];
                    Array.Copy(tempArray, result, validCount);
                    return result;
                }
                finally
                {
                    ArrayPool<Type>.Shared.Return(tempArray);
                }
            }
            catch (Exception)
            {
                return [];
            }
        });

        if (types.Length == 0)
        {
            yield break;
        }

        var filteredTypes = types.Where(t => t.IsClass && !IsCompilerGenerated(t));

        foreach (var type in filteredTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip abstract types - they can't be instantiated
            if (type.IsAbstract)
            {
                continue;
            }

            // Handle generic type definitions specially
            if (type.IsGenericTypeDefinition)
            {
                await foreach (var genericTest in DiscoverGenericTestsStreamingAsync(type, cancellationToken))
                {
                    yield return genericTest;
                }
                continue;
            }

            MethodInfo[] testMethods;
            try
            {
                // Check if this class inherits tests from base classes
                var inheritsTests = type.IsDefined(typeof(InheritsTestsAttribute), inherit: false);

                if (inheritsTests)
                {
                    // Get all test methods including inherited ones
                    testMethods = GetAllTestMethods(type)
                        .Where(m => m.IsDefined(typeof(TestAttribute), inherit: false) && !m.IsAbstract)
                        .ToArray();
                }
                else
                {
                    // Only get declared test methods
                    testMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(m => m.IsDefined(typeof(TestAttribute), inherit: false) && !m.IsAbstract)
                        .ToArray();
                }
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var method in testMethods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                TestMetadata? testMetadata = null;
                TestMetadata? failedMetadata = null;

                try
                {
                    // Prevent duplicate test metadata for inherited tests
                    if (method.DeclaringType != type && !type.IsDefined(typeof(InheritsTestsAttribute), inherit: false))
                    {
                        continue;
                    }

                    testMetadata = await BuildTestMetadata(type, method).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Create a failed test metadata for discovery failures
                    failedMetadata = CreateFailedTestMetadata(type, method, ex);
                }

                if (testMetadata != null)
                {
                    yield return testMetadata;
                }
                else if (failedMetadata != null)
                {
                    yield return failedMetadata;
                }
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2055:Call to 'System.Type.MakeGenericType' can not be statically analyzed",
        Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2067:'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'System.Activator.CreateInstance(Type)'",
        Justification = "Reflection mode requires dynamic access")]
    private static async Task<List<TestMetadata>> DiscoverGenericTests(Type genericTypeDefinition)
    {
        var discoveredTests = new List<TestMetadata>();

        // Extract class-level data sources that will determine the generic type arguments
        var classDataSources = ReflectionAttributeExtractor.ExtractDataSources(genericTypeDefinition);

        if (classDataSources.Length == 0)
        {
            // This is expected for generic test classes in reflection mode
            // They need data sources to determine concrete types
            return discoveredTests;
        }

        // Get test methods from the generic type definition
        // Optimize: Manual filtering instead of LINQ Where().ToArray()
        var declaredMethods = genericTypeDefinition.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var testMethodsList = new List<MethodInfo>(declaredMethods.Length);
        foreach (var method in declaredMethods)
        {
            if (method.IsDefined(typeof(TestAttribute), inherit: false) && !method.IsAbstract)
            {
                testMethodsList.Add(method);
            }
        }
        var testMethods = testMethodsList.ToArray();

        if (testMethods.Length == 0)
        {
            return discoveredTests;
        }

        // For each data source combination, create a concrete generic type
        foreach (var dataSource in classDataSources)
        {
            var dataItems = await GetDataFromSourceAsync(dataSource, null!).ConfigureAwait(false); // TODO

            foreach (var dataRow in dataItems)
            {
                if (dataRow == null || dataRow.Length == 0)
                {
                    continue;
                }

                // Determine generic type arguments from the data
                var typeArguments = ReflectionGenericTypeResolver.DetermineGenericTypeArguments(genericTypeDefinition, dataRow);
                if (typeArguments == null || typeArguments.Length == 0)
                {
                    continue;
                }

                try
                {
                    // Create concrete type with validation
                    var concreteType = ReflectionGenericTypeResolver.CreateConcreteType(genericTypeDefinition, typeArguments);

                    // Build tests for each method in the concrete type
                    foreach (var genericMethod in testMethods)
                    {
                        var concreteMethod = concreteType.GetMethod(genericMethod.Name,
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

                        if (concreteMethod != null)
                        {
                            // Build test metadata for the concrete type
                            // The concrete type already has its generic arguments resolved
                            // For generic types with primary constructors that were resolved from class-level data sources,
                            // we need to ensure the class data sources contain the specific data for this instantiation
                            var testMetadata = await BuildTestMetadata(concreteType, concreteMethod, dataRow).ConfigureAwait(false);

                            discoveredTests.Add(testMetadata);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to create concrete type for {genericTypeDefinition.FullName ?? genericTypeDefinition.Name}. " +
                        $"Error: {ex.Message}. " +
                        $"Generic parameter count: {genericTypeDefinition.GetGenericArguments().Length}, " +
                        $"Type arguments provided: {typeArguments?.Length ?? 0}, " +
                        $"Data row length: {dataRow?.Length ?? 0}", ex);
                }
            }
        }

        return discoveredTests;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2055:Call to 'System.Type.MakeGenericType' can not be statically analyzed",
        Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2067:'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'System.Activator.CreateInstance(Type)'",
        Justification = "Reflection mode requires dynamic access")]
    private static async IAsyncEnumerable<TestMetadata> DiscoverGenericTestsStreamingAsync(
        Type genericTypeDefinition,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Extract class-level data sources that will determine the generic type arguments
        var classDataSources = ReflectionAttributeExtractor.ExtractDataSources(genericTypeDefinition);

        if (classDataSources.Length == 0)
        {
            // This is expected for generic test classes in reflection mode
            // They need data sources to determine concrete types
            yield break;
        }

        // Get test methods from the generic type definition
        // Optimize: Manual filtering instead of LINQ Where().ToArray()
        var declaredMethods = genericTypeDefinition.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var testMethodsList = new List<MethodInfo>(declaredMethods.Length);
        foreach (var method in declaredMethods)
        {
            if (method.IsDefined(typeof(TestAttribute), inherit: false) && !method.IsAbstract)
            {
                testMethodsList.Add(method);
            }
        }
        var testMethods = testMethodsList.ToArray();

        if (testMethods.Length == 0)
        {
            yield break;
        }

        // For each data source combination, create a concrete generic type
        foreach (var dataSource in classDataSources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dataItems = await GetDataFromSourceAsync(dataSource, null!).ConfigureAwait(false); // TODO

            foreach (var dataRow in dataItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (dataRow == null || dataRow.Length == 0)
                {
                    continue;
                }

                // Determine generic type arguments from the data
                var typeArguments = ReflectionGenericTypeResolver.DetermineGenericTypeArguments(genericTypeDefinition, dataRow);
                if (typeArguments == null || typeArguments.Length == 0)
                {
                    continue;
                }

                TestMetadata? failedMetadata = null;
                List<TestMetadata>? successfulTests = null;

                try
                {
                    // Create concrete type with validation
                    var concreteType = ReflectionGenericTypeResolver.CreateConcreteType(genericTypeDefinition, typeArguments);

                    // Build tests for each method in the concrete type
                    foreach (var genericMethod in testMethods)
                    {
                        var concreteMethod = concreteType.GetMethod(genericMethod.Name,
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

                        if (concreteMethod != null)
                        {
                            // Build test metadata for the concrete type
                            // The concrete type already has its generic arguments resolved
                            // For generic types with primary constructors that were resolved from class-level data sources,
                            // we need to ensure the class data sources contain the specific data for this instantiation
                            var testMetadata = await BuildTestMetadata(concreteType, concreteMethod, dataRow).ConfigureAwait(false);

                            if (successfulTests == null)
                            {
                                successfulTests =
                                [
                                ];
                            }
                            successfulTests.Add(testMetadata);
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedMetadata = new FailedTestMetadata(
                        new InvalidOperationException(
                            $"Failed to create concrete type for {genericTypeDefinition.FullName ?? genericTypeDefinition.Name}. " +
                            $"Error: {ex.Message}. " +
                            $"Generic parameter count: {genericTypeDefinition.GetGenericArguments().Length}, " +
                            $"Type arguments: {string.Join(", ", typeArguments?.Select(t => t.Name) ?? [
                            ])}", ex),
                        $"[GENERIC TYPE CREATION FAILED] {genericTypeDefinition.Name}")
                    {
                        TestName = $"[GENERIC TYPE CREATION FAILED] {genericTypeDefinition.Name}",
                        TestClassType = genericTypeDefinition,
                        TestMethodName = "GenericTypeCreationFailed",
                        FilePath = "Unknown",
                        LineNumber = 0,
                        MethodMetadata = CreateDummyMethodMetadata(genericTypeDefinition, "GenericTypeCreationFailed"),
                        AttributeFactory = () => [],
                        DataSources = [],
                        ClassDataSources = [],
                        PropertyDataSources = []
                    };
                }

                // Yield successful tests first
                if (successfulTests != null)
                {
                    foreach (var test in successfulTests)
                    {
                        yield return test;
                    }
                }

                // Then yield failed metadata if any
                if (failedMetadata != null)
                {
                    yield return failedMetadata;
                }
            }
        }
    }


    private static async Task<List<object?[]>> GetDataFromSourceAsync(IDataSourceAttribute dataSource, MethodMetadata methodMetadata)
    {
        var data = new List<object?[]>();

        try
        {
            // Use the centralized factory for generic type discovery
            var metadata = DataGeneratorMetadataCreator.CreateForGenericTypeDiscovery(dataSource, methodMetadata);

            // Get data rows from the source
            await foreach (var rowFactory in dataSource.GetDataRowsAsync(metadata))
            {
                var dataArray = await rowFactory().ConfigureAwait(false);
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

        return await Task.FromResult(data).ConfigureAwait(false);
    }

    private static int CalculateInheritanceDepth(Type testClass, MethodInfo testMethod)
    {
        // If the method is declared directly in the test class, depth is 0
        if (testMethod.DeclaringType == testClass)
        {
            return 0;
        }

        // Count how many levels up the inheritance chain the method is declared
        int depth = 0;
        Type? currentType = testClass.BaseType;

        while (currentType != null && currentType != typeof(object))
        {
            depth++;
            if (testMethod.DeclaringType == currentType)
            {
                return depth;
            }
            currentType = currentType.BaseType;
        }

        // This shouldn't happen in normal cases, but return the depth anyway
        return depth;
    }

    private static Task<TestMetadata> BuildTestMetadata(Type testClass, MethodInfo testMethod, object?[]? classData = null)
    {
        // Create a base ReflectionTestMetadata instance
        var testName = GenerateTestName(testClass, testMethod);

        // Calculate inheritance depth
        int inheritanceDepth = CalculateInheritanceDepth(testClass, testMethod);

        try
        {
            return Task.FromResult<TestMetadata>(new ReflectionTestMetadata(testClass, testMethod)
            {
                TestName = testName,
                TestClassType = testClass,
                TestMethodName = testMethod.Name,
                Dependencies = ReflectionAttributeExtractor.ExtractDependencies(testClass, testMethod),
                DataSources = ReflectionAttributeExtractor.ExtractDataSources(testMethod),
                ClassDataSources = classData != null
                    ? [new StaticDataSourceAttribute(new[] { classData })]
                    : ReflectionAttributeExtractor.ExtractDataSources(testClass),
                PropertyDataSources = ReflectionAttributeExtractor.ExtractPropertyDataSources(testClass),
                InstanceFactory = CreateInstanceFactory(testClass)!,
                TestInvoker = CreateTestInvoker(testClass, testMethod),
                FilePath = ExtractFilePath(testMethod) ?? "Unknown",
                LineNumber = ExtractLineNumber(testMethod) ?? 0,
                MethodMetadata = ReflectionMetadataBuilder.CreateMethodMetadata(testClass, testMethod),
                GenericTypeInfo = ReflectionGenericTypeResolver.ExtractGenericTypeInfo(testClass),
                GenericMethodInfo = ReflectionGenericTypeResolver.ExtractGenericMethodInfo(testMethod),
                GenericMethodTypeArguments = testMethod.IsGenericMethodDefinition ? null : testMethod.GetGenericArguments(),
                AttributeFactory = () => ReflectionAttributeExtractor.GetAllAttributes(testClass, testMethod),
                PropertyInjections = PropertyInjectionService.DiscoverInjectableProperties(testClass),
                InheritanceDepth = inheritanceDepth
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(CreateFailedTestMetadata(testClass, testMethod, ex));
        }
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
                BindingFlags.NonPublic | BindingFlags.Instance);
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

        // Default format - just method name to match source generation
        return testMethod.Name;
    }



    [UnconditionalSuppressMessage("Trimming",
        "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' in call to 'System.Type.GetConstructors()'",
        Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2067:Target parameter does not satisfy annotation requirements",
        Justification = "Reflection mode requires dynamic access")]
    private static Func<Type[], object?[], object> CreateInstanceFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type testClass)
    {
        // For generic types, we need to handle MakeGenericType
        if (testClass.IsGenericTypeDefinition)
        {
            return (typeArgs, args) =>
            {
                if (typeArgs == null || typeArgs.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Cannot create instance of generic type definition {testClass.FullName} without type arguments.");
                }

                if (typeArgs.Length != testClass.GetGenericArguments().Length)
                {
                    throw new InvalidOperationException(
                        $"Type argument count mismatch for {testClass.FullName}: expected {testClass.GetGenericArguments().Length}, got {typeArgs.Length}");
                }

                var closedType = testClass.MakeGenericType(typeArgs);
                if (args.Length == 0)
                {
                    return Activator.CreateInstance(closedType)!;
                }
                return Activator.CreateInstance(closedType, args)!;
            };
        }

        // For already-constructed generic types (e.g., from DiscoverGenericTests)
        // we don't need type arguments - the type is already closed
        if (testClass.IsConstructedGenericType)
        {
            var constructedTypeConstructors = testClass.GetConstructors();
            if (constructedTypeConstructors.Length == 0)
            {
                return (_, _) => Activator.CreateInstance(testClass)!;
            }

            var constructedTypeCtor = constructedTypeConstructors.FirstOrDefault(c => c.GetParameters().Length == 0) ?? constructedTypeConstructors.First();
            var constructedTypeFactory = CreateReflectionInstanceFactory(constructedTypeCtor);

            // Return a factory that ignores type arguments since the type is already closed
            return (_, args) => constructedTypeFactory(args);
        }

        var constructors = testClass.GetConstructors();

        if (constructors.Length == 0)
        {
            return (_, _) => Activator.CreateInstance(testClass)!;
        }

        var ctor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0) ?? constructors.First();

        var factory = CreateReflectionInstanceFactory(ctor);
        return (_, args) => factory(args);
    }


    private static Func<object, object?[], Task> CreateTestInvoker(Type testClass, MethodInfo testMethod)
    {
        return CreateReflectionTestInvoker(testClass, testMethod);
    }


    // Hook discovery has been separated into ReflectionHookDiscoveryService

    private static bool IsAsyncMethod(MethodInfo method)
    {
        return method.ReturnType == typeof(Task) ||
            method.ReturnType == typeof(ValueTask) ||
            (method.ReturnType.IsGenericType &&
                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
    }

    private static bool IsCompilerGenerated(Type type)
    {
        // If the type is not marked as compiler-generated, it's not compiler-generated
        if (!type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
        {
            return false;
        }
        
        // If the type is compiler-generated but contains test methods, allow it
        // This handles cases like Reqnroll-generated test classes that should be executed
        return !HasTestMethods(type);
    }
    
    private static bool HasTestMethods(Type type)
    {
        try
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                if (method.IsDefined(typeof(TestAttribute), inherit: false))
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            // If we can't access the methods, treat it as not having test methods
            return false;
        }
    }

    private static ParameterInfo[] GetParametersWithoutCancellationToken(MethodInfo method)
    {
        var parameters = method.GetParameters();

        // Check if last parameter is CancellationToken and exclude it
        if (parameters.Length > 0 && parameters[^1].ParameterType == typeof(CancellationToken))
        {
            // Optimize: Manual array copy instead of LINQ Take().ToArray()
            var result = new ParameterInfo[parameters.Length - 1];
            Array.Copy(parameters, result, parameters.Length - 1);
            return result;
        }

        return parameters;
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
            FilePath = "Unknown",
            LineNumber = 0,
            MethodMetadata = CreateDummyMethodMetadata(testClass,
                "AssemblyScanFailed"),
            AttributeFactory = () =>
            [
            ],
            DataSources =
            [
            ],
            ClassDataSources =
            [
            ],
            PropertyDataSources =
            [
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
            FilePath = ExtractFilePath(method) ?? "Unknown",
            LineNumber = ExtractLineNumber(method) ?? 0,
            MethodMetadata = ReflectionMetadataBuilder.CreateMethodMetadata(type, method),
            AttributeFactory = () => method.GetCustomAttributes()
                .ToArray(),
            DataSources =
            [
            ],
            ClassDataSources =
            [
            ],
            PropertyDataSources =
            [
            ]
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
                Assembly = new AssemblyMetadata
                {
                    Name = type.Assembly.GetName().Name ?? "Unknown"
                },
                Parameters =
                [
                ],
                Properties =
                [
                ],
                Parent = null
            },
            Parameters =
            [
            ],
            GenericTypeCount = 0,
            ReturnTypeReference = TypeReference.CreateConcrete(typeof(void).AssemblyQualifiedName!),
            ReturnType = typeof(void),
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName!)
        };
    }

    private sealed class FailedTestMetadata : TestMetadata
    {
        private readonly Exception _exception;
        private readonly string _displayName;

        public FailedTestMetadata(Exception exception, string displayName)
        {
            _exception = exception;
            _displayName = displayName;
        }

        public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
        {
            get => (context, metadata) => new FailedExecutableTest(_exception)
            {
                TestId = context.TestId,
                Metadata = metadata,
                Arguments = context.Arguments,
                ClassArguments = context.ClassArguments,
                Context = context.Context
            };
        }
    }


    /// <summary>
    /// Creates a reflection-based instance factory with proper AOT attribution
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    private static Func<object?[], object> CreateReflectionInstanceFactory(ConstructorInfo ctor)
    {
        var isPrepared = false;

        return args =>
        {
            try
            {
                // Pre-JIT on first actual invocation for better performance
                if (!isPrepared)
                {
                    RuntimeHelpers.PrepareMethod(ctor.MethodHandle);
                    isPrepared = true;
                }

                // Cast arguments to the expected parameter types
                var parameters = ctor.GetParameters();
                var castedArgs = new object?[parameters.Length];

                for (int i = 0; i < parameters.Length && i < args.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    var arg = args[i];

                    if (arg == null)
                    {
                        castedArgs[i] = null;
                        continue;
                    }

                    var argType = arg.GetType();

                    // If the argument is already assignable to the parameter type, use it directly
                    // This handles delegates and other non-convertible types
                    if (paramType.IsAssignableFrom(argType))
                    {
                        castedArgs[i] = arg;
                    }
                    // Special handling for covariant interfaces like IEnumerable<T>
                    else if (IsCovariantCompatible(paramType, argType))
                    {
                        castedArgs[i] = arg;
                    }
                    else
                    {
                        // Otherwise use CastHelper for conversions
                        castedArgs[i] = CastHelper.Cast(paramType, arg);
                    }
                }

                return ctor.Invoke(castedArgs) ?? throw new InvalidOperationException("Failed to create instance");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of {ctor.DeclaringType?.Name}", ex);
            }
        };
    }

    /// <summary>
    /// Infers generic type mappings from parameter and argument types
    /// </summary>
    private static void InferGenericTypeMapping(Type paramType, Type argType, Dictionary<Type, Type> typeMapping)
    {
        // Direct generic parameter
        if (paramType.IsGenericParameter)
        {
            // Check if we already have a mapping for this parameter
            if (typeMapping.TryGetValue(paramType, out var existingType))
            {
                // If the existing type is more specific (derived from current), keep it
                if (existingType.IsAssignableFrom(argType))
                {
                    typeMapping[paramType] = argType;
                }
                // Otherwise keep the more general type
            }
            else
            {
                typeMapping[paramType] = argType;
            }
            return;
        }

        // Generic types (e.g., IEnumerable<T>, Func<T1,T2>, etc.)
        if (paramType.IsGenericType && argType.IsGenericType)
        {
            var paramGenDef = paramType.GetGenericTypeDefinition();

            // Check if argument type directly matches or implements the parameter generic type
            if (argType.GetGenericTypeDefinition() == paramGenDef)
            {
                var paramGenArgs = paramType.GetGenericArguments();
                var argGenArgs = argType.GetGenericArguments();

                for (int i = 0; i < paramGenArgs.Length && i < argGenArgs.Length; i++)
                {
                    InferGenericTypeMapping(paramGenArgs[i], argGenArgs[i], typeMapping);
                }
            }
            else
            {
                // Check interfaces implemented by the argument type
                foreach (var iface in argType.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == paramGenDef)
                    {
                        var paramGenArgs = paramType.GetGenericArguments();
                        var ifaceGenArgs = iface.GetGenericArguments();

                        for (int i = 0; i < paramGenArgs.Length && i < ifaceGenArgs.Length; i++)
                        {
                            InferGenericTypeMapping(paramGenArgs[i], ifaceGenArgs[i], typeMapping);
                        }
                        break;
                    }
                }
            }
        }
        else if (paramType.IsGenericType && !argType.IsGenericType)
        {
            // Handle non-generic types that implement generic interfaces
            var paramGenDef = paramType.GetGenericTypeDefinition();

            foreach (var iface in argType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == paramGenDef)
                {
                    var paramGenArgs = paramType.GetGenericArguments();
                    var ifaceGenArgs = iface.GetGenericArguments();

                    for (int i = 0; i < paramGenArgs.Length && i < ifaceGenArgs.Length; i++)
                    {
                        InferGenericTypeMapping(paramGenArgs[i], ifaceGenArgs[i], typeMapping);
                    }
                    break;
                }
            }
        }

        // Array types
        if (paramType.IsArray && argType.IsArray)
        {
            InferGenericTypeMapping(paramType.GetElementType()!, argType.GetElementType()!, typeMapping);
        }
    }

    /// <summary>
    /// Checks if the argument type is compatible with the parameter type through covariance
    /// </summary>
    private static bool IsCovariantCompatible(Type paramType, Type argType)
    {
        // Only check for generic interface covariance
        if (!paramType.IsInterface || !paramType.IsGenericType)
        {
            return false;
        }

        var paramGenericDef = paramType.GetGenericTypeDefinition();

        // List of known covariant interfaces
        var covariantInterfaces = new[]
        {
            typeof(IEnumerable<>),
            typeof(IReadOnlyList<>),
            typeof(IReadOnlyCollection<>),
            typeof(IEnumerator<>)
        };

        if (!covariantInterfaces.Contains(paramGenericDef))
        {
            return false;
        }

        // Get all interfaces from the argument type
        var argInterfaces = argType.GetInterfaces();
        if (argType.IsInterface)
        {
            argInterfaces = argInterfaces.Concat([argType]).ToArray();
        }

        foreach (var iface in argInterfaces)
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == paramGenericDef)
            {
                var paramElementType = paramType.GetGenericArguments()[0];
                var argElementType = iface.GetGenericArguments()[0];

                // For covariance to work, the parameter element type must be assignable from the argument element type
                // This allows IEnumerable<int> to be passed where IEnumerable<object> is expected
                return paramElementType.IsAssignableFrom(argElementType);
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a reflection-based test invoker with proper AOT attribution
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Reflection mode cannot support AOT")]
    private static Func<object, object?[], Task> CreateReflectionTestInvoker(Type testClass, MethodInfo testMethod)
    {
        var isPrepared = false;

        return (instance, args) =>
        {
            try
            {
                // Get the method to invoke - may need to make it concrete if it's generic
                var methodToInvoke = testMethod;

                // Pre-JIT on first actual invocation for better performance
                if (!isPrepared && !testMethod.IsGenericMethodDefinition)
                {
                    RuntimeHelpers.PrepareMethod(testMethod.MethodHandle);
                    isPrepared = true;
                }

                // If the method is a generic method definition, we need to make it concrete
                if (testMethod.IsGenericMethodDefinition)
                {
                    // Try to infer type arguments from the actual arguments
                    var genericParams = testMethod.GetGenericArguments();
                    var typeMapping = new Dictionary<Type, Type>();
                    var methodParams = testMethod.GetParameters();

                    // Build type mapping from method parameters and actual arguments
                    for (int j = 0; j < methodParams.Length && j < args.Length; j++)
                    {
                        var arg = args[j];
                        if (arg != null)
                        {
                            var paramType = methodParams[j].ParameterType;
                            var argType = arg.GetType();
                            InferGenericTypeMapping(paramType, argType, typeMapping);
                        }
                    }

                    // Create type arguments array from the mapping
                    var typeArguments = new Type[genericParams.Length];
                    for (int i = 0; i < genericParams.Length; i++)
                    {
                        if (typeMapping.TryGetValue(genericParams[i], out var inferredType))
                        {
                            typeArguments[i] = inferredType;
                        }
                        else
                        {
                            // If we couldn't infer the type, default to object
                            typeArguments[i] = typeof(object);
                        }
                    }

                    methodToInvoke = testMethod.MakeGenericMethod(typeArguments);

                    // Pre-JIT the constructed generic method on first invocation
                    RuntimeHelpers.PrepareMethod(methodToInvoke.MethodHandle);
                }

                // Cast arguments to the expected parameter types
                var parameters = methodToInvoke.GetParameters();
                var castedArgs = new object?[parameters.Length];

                for (int i = 0; i < parameters.Length && i < args.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    var arg = args[i];

                    if (arg == null)
                    {
                        castedArgs[i] = null;
                        continue;
                    }

                    var argType = arg.GetType();

                    // If the argument is already assignable to the parameter type, use it directly
                    // This handles delegates and other non-convertible types
                    if (paramType.IsAssignableFrom(argType))
                    {
                        castedArgs[i] = arg;
                    }
                    // Special handling for covariant interfaces like IEnumerable<T>
                    else if (IsCovariantCompatible(paramType, argType))
                    {
                        castedArgs[i] = arg;
                    }
                    else
                    {
                        // Otherwise use CastHelper for conversions
                        castedArgs[i] = CastHelper.Cast(paramType, arg);
                    }
                }

                var result = methodToInvoke.Invoke(instance, castedArgs);
                if (result is Task task)
                {
                    return task;
                }
                if (result is ValueTask valueTask)
                {
                    return valueTask.AsTask();
                }
                return Task.CompletedTask;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException ?? tie).Throw();
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        };
    }

    private async Task<List<TestMetadata>> DiscoverDynamicTests(string testSessionId)
    {
        var dynamicTests = new List<TestMetadata>();

        // First check if there are any registered dynamic test sources from source generation
        if (Sources.DynamicTestSources.Count > 0)
        {
            foreach (var source in Sources.DynamicTestSources)
            {
                try
                {
                    var tests = source.CollectDynamicTests(testSessionId);
                    foreach (var dynamicTest in tests)
                    {
                        var testMetadataList = await ConvertDynamicTestToMetadata(dynamicTest).ConfigureAwait(false);
                        dynamicTests.AddRange(testMetadataList);
                    }
                }
                catch (Exception ex)
                {
                    // Create a failed test metadata for this dynamic test source
                    var failedTest = CreateFailedTestMetadataForDynamicSource(source, ex);
                    dynamicTests.Add(failedTest);
                }
            }
        }

        // Also discover dynamic test builder methods via reflection
        // Optimize: Pre-filter and allocate array instead of LINQ ToList()
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var assembliesList = new List<Assembly>(allAssemblies.Length);
        foreach (var assembly in allAssemblies)
        {
            if (ShouldScanAssembly(assembly))
            {
                assembliesList.Add(assembly);
            }
        }
        var assemblies = assembliesList;

        foreach (var assembly in assemblies)
        {
            var types = _assemblyTypesCache.GetOrAdd(assembly, asm =>
            {
                try
                {
                    return asm.GetExportedTypes();
                }
                catch
                {
                    return [];
                }
            });

            foreach (var type in types.Where(t => t.IsClass && !IsCompilerGenerated(t)))
            {
                // Optimize: Manual filtering instead of LINQ Where().ToArray()
                var declaredMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                var methodsList = new List<MethodInfo>(declaredMethods.Length);
                foreach (var method in declaredMethods)
                {
#pragma warning disable TUnitWIP0001
                    if (method.IsDefined(typeof(DynamicTestBuilderAttribute), inherit: false) && !method.IsAbstract)
#pragma warning restore TUnitWIP0001
                    {
                        methodsList.Add(method);
                    }
                }
                var methods = methodsList.ToArray();

                foreach (var method in methods)
                {
                    try
                    {
                        var tests = await ExecuteDynamicTestBuilder(type, method, testSessionId).ConfigureAwait(false);
                        dynamicTests.AddRange(tests);
                    }
                    catch (Exception ex)
                    {
                        // Create a failed test metadata for this dynamic test builder
                        var failedTest = CreateFailedTestMetadataForDynamicBuilder(type, method, ex);
                        dynamicTests.Add(failedTest);
                    }
                }
            }
        }

        return dynamicTests;
    }

    private async IAsyncEnumerable<TestMetadata> DiscoverDynamicTestsStreamingAsync(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(ShouldScanAssembly)
            .ToList();

        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var types = _assemblyTypesCache.GetOrAdd(assembly, asm =>
            {
                try
                {
                    return asm.GetExportedTypes();
                }
                catch
                {
                    return [];
                }
            });

            foreach (var type in types.Where(t => t.IsClass && !IsCompilerGenerated(t)))
            {
                // Optimize: Manual filtering instead of LINQ Where().ToArray()
                var declaredMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                var methodsList = new List<MethodInfo>(declaredMethods.Length);
                foreach (var method in declaredMethods)
                {
#pragma warning disable TUnitWIP0001
                    if (method.IsDefined(typeof(DynamicTestBuilderAttribute), inherit: false) && !method.IsAbstract)
#pragma warning restore TUnitWIP0001
                    {
                        methodsList.Add(method);
                    }
                }
                var methods = methodsList.ToArray();

                foreach (var method in methods)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Stream tests from this dynamic builder
                    await foreach (var test in ExecuteDynamicTestBuilderStreamingAsync(type, method, testSessionId, cancellationToken))
                    {
                        yield return test;
                    }
                }
            }
        }
    }

    private async Task<List<TestMetadata>> ExecuteDynamicTestBuilder(Type testClass, MethodInfo builderMethod, string testSessionId)
    {
        var dynamicTests = new List<TestMetadata>();

        // Extract file path and line number from the DynamicTestBuilderAttribute if possible
        var filePath = ExtractFilePath(builderMethod) ?? "Unknown";
        var lineNumber = ExtractLineNumber(builderMethod) ?? 0;

        // Create context
        var context = new DynamicTestBuilderContext(filePath, lineNumber);

        // Create instance if needed
        object? instance = null;
        if (!builderMethod.IsStatic)
        {
            instance = Activator.CreateInstance(testClass);
        }

        // Execute the builder method
        builderMethod.Invoke(instance, [context]);

        // Convert the dynamic tests to test metadata
        foreach (var dynamicTest in context.Tests)
        {
            var testMetadataList = await ConvertDynamicTestToMetadata(dynamicTest).ConfigureAwait(false);
            dynamicTests.AddRange(testMetadataList);
        }

        return dynamicTests;
    }

    private async Task<List<TestMetadata>> ConvertDynamicTestToMetadata(DynamicTest dynamicTest)
    {
        var testMetadataList = new List<TestMetadata>();

        foreach (var discoveryResult in dynamicTest.GetTests())
        {
            if (discoveryResult is DynamicDiscoveryResult { TestMethod: not null } dynamicResult)
            {
                var testMetadata = await CreateMetadataFromDynamicDiscoveryResult(dynamicResult).ConfigureAwait(false);
                testMetadataList.Add(testMetadata);
            }
        }

        return testMetadataList;
    }

    private async IAsyncEnumerable<TestMetadata> ExecuteDynamicTestBuilderStreamingAsync(
        Type testClass, MethodInfo builderMethod, string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        TestMetadata? failedMetadata = null;
        List<TestMetadata>? successfulTests = null;

        try
        {
            // Extract file path and line number from the DynamicTestBuilderAttribute if possible
            var filePath = ExtractFilePath(builderMethod) ?? "Unknown";
            var lineNumber = ExtractLineNumber(builderMethod) ?? 0;

            // Create context
            var context = new DynamicTestBuilderContext(filePath, lineNumber);

            // Create instance if needed
            object? instance = null;
            if (!builderMethod.IsStatic)
            {
                instance = Activator.CreateInstance(testClass);
            }

            // Invoke the builder method
            builderMethod.Invoke(instance, [context]);

            // Retrieve the discovered tests
            foreach (var discoveryResult in context.Tests.SelectMany(t => t.GetTests()))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (discoveryResult is DynamicDiscoveryResult dynamicResult)
                {
                    try
                    {
                        var testMetadata = await CreateMetadataFromDynamicDiscoveryResult(dynamicResult).ConfigureAwait(false);
                        if (successfulTests == null)
                        {
                            successfulTests =
                            [
                            ];
                        }
                        successfulTests.Add(testMetadata);
                    }
                    catch (Exception ex)
                    {
                        // Create a failed test metadata for this specific test
                        var failedTest = CreateFailedTestMetadataForDynamicTest(dynamicResult, ex);
                        if (successfulTests == null)
                        {
                            successfulTests =
                            [
                            ];
                        }
                        successfulTests.Add(failedTest);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Create a failed test metadata for this dynamic test builder
            failedMetadata = CreateFailedTestMetadataForDynamicBuilder(testClass, builderMethod, ex);
        }

        // Yield successful tests
        if (successfulTests != null)
        {
            foreach (var test in successfulTests)
            {
                yield return test;
            }
        }

        // Yield failed metadata if any
        if (failedMetadata != null)
        {
            yield return failedMetadata;
        }
    }

    private Task<TestMetadata> CreateMetadataFromDynamicDiscoveryResult(DynamicDiscoveryResult result)
    {
        if (result.TestClassType == null || result.TestMethod == null)
        {
            throw new InvalidOperationException("Dynamic test discovery result must have a test class type and method");
        }

        // Extract method info from the expression
        MethodInfo? methodInfo = null;
        var lambdaExpression = result.TestMethod as System.Linq.Expressions.LambdaExpression;
        if (lambdaExpression?.Body is System.Linq.Expressions.MethodCallExpression methodCall)
        {
            methodInfo = methodCall.Method;
        }
        else if (lambdaExpression?.Body is System.Linq.Expressions.UnaryExpression { Operand: System.Linq.Expressions.MethodCallExpression unaryMethodCall })
        {
            methodInfo = unaryMethodCall.Method;
        }

        if (methodInfo == null)
        {
            throw new InvalidOperationException("Could not extract method info from dynamic test expression");
        }

        var testName = GenerateTestName(result.TestClassType, methodInfo);

        var metadata = new DynamicReflectionTestMetadata(result.TestClassType, methodInfo, result)
        {
            TestName = testName,
            TestClassType = result.TestClassType,
            TestMethodName = methodInfo.Name,
            Dependencies = result.Attributes.OfType<DependsOnAttribute>().Select(a => a.ToTestDependency()).ToArray(),
            DataSources = [], // Dynamic tests don't use data sources in the same way
            ClassDataSources = [],
            PropertyDataSources = [],
            InstanceFactory = CreateDynamicInstanceFactory(result.TestClassType, result.TestClassArguments)!,
            TestInvoker = CreateDynamicTestInvoker(result),
            FilePath = result.CreatorFilePath ?? "Unknown",
            LineNumber = result.CreatorLineNumber ?? 0,
            MethodMetadata = ReflectionMetadataBuilder.CreateMethodMetadata(result.TestClassType, methodInfo),
            GenericTypeInfo = ReflectionGenericTypeResolver.ExtractGenericTypeInfo(result.TestClassType),
            GenericMethodInfo = ReflectionGenericTypeResolver.ExtractGenericMethodInfo(methodInfo),
            GenericMethodTypeArguments = methodInfo.IsGenericMethodDefinition ? null : methodInfo.GetGenericArguments(),
            AttributeFactory = () => result.Attributes.ToArray(),
            PropertyInjections = PropertyInjectionService.DiscoverInjectableProperties(result.TestClassType)
        };

        return Task.FromResult<TestMetadata>(metadata);
    }

    private static Func<Type[], object?[], object> CreateDynamicInstanceFactory(Type testClass, object?[]? predefinedClassArgs)
    {
        // For dynamic tests, we always use the predefined args (or empty array if null)
        var classArgs = predefinedClassArgs ?? [];

        return (typeArgs, args) =>
        {
            // Always use the predefined class args, ignoring the args parameter
            if (testClass.IsGenericTypeDefinition && typeArgs.Length > 0)
            {
                var closedType = testClass.MakeGenericType(typeArgs);
                if (classArgs.Length == 0)
                {
                    return Activator.CreateInstance(closedType)!;
                }
                return Activator.CreateInstance(closedType, classArgs)!;
            }

            if (classArgs.Length == 0)
            {
                return Activator.CreateInstance(testClass)!;
            }
            return Activator.CreateInstance(testClass, classArgs)!;
        };
    }

    private static Func<object, object?[], Task> CreateDynamicTestInvoker(DynamicDiscoveryResult result)
    {
        return async (instance, args) =>
        {
            try
            {
                if (result.TestMethod == null)
                {
                    throw new InvalidOperationException("Dynamic test method expression is null");
                }

                // Since we're in reflection mode, we can compile and invoke the expression
                var lambdaExpression = result.TestMethod as System.Linq.Expressions.LambdaExpression;
                if (lambdaExpression == null)
                {
                    throw new InvalidOperationException("Dynamic test method must be a lambda expression");
                }

                var compiledExpression = lambdaExpression.Compile();
                var testInstance = instance ?? throw new InvalidOperationException("Test instance is null");

                // The expression is already bound to the correct method with arguments
                // so we just need to invoke it with the instance
                var invokeMethod = compiledExpression.GetType().GetMethod("Invoke")!;
                var invokeResult = invokeMethod.Invoke(compiledExpression, [testInstance]);

                if (invokeResult is Task task)
                {
                    await task.ConfigureAwait(false);
                }
                else if (invokeResult is ValueTask valueTask)
                {
                    await valueTask.ConfigureAwait(false);
                }
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException ?? tie).Throw();
                throw;
            }
        };
    }

    private static TestMetadata CreateFailedTestMetadataForDynamicSource(IDynamicTestSource source, Exception ex)
    {
        var testName = $"[DYNAMIC SOURCE FAILED] {source.GetType().Name}";
        var displayName = $"{testName} - {ex.Message}";

        return new FailedTestMetadata(ex, displayName)
        {
            TestName = testName,
            TestClassType = source.GetType(),
            TestMethodName = "CollectDynamicTests",
            FilePath = "Unknown",
            LineNumber = 0,
            MethodMetadata = CreateDummyMethodMetadata(source.GetType(), "CollectDynamicTests"),
            AttributeFactory = () => [],
            DataSources = [],
            ClassDataSources = [],
            PropertyDataSources = []
        };
    }

    private static TestMetadata CreateFailedTestMetadataForDynamicBuilder(Type type, MethodInfo method, Exception ex)
    {
        var testName = $"[DYNAMIC BUILDER FAILED] {type.FullName}.{method.Name}";
        var displayName = $"{testName} - {ex.Message}";

        return new FailedTestMetadata(ex, displayName)
        {
            TestName = testName,
            TestClassType = type,
            TestMethodName = method.Name,
            FilePath = ExtractFilePath(method) ?? "Unknown",
            LineNumber = ExtractLineNumber(method) ?? 0,
            MethodMetadata = ReflectionMetadataBuilder.CreateMethodMetadata(type, method),
            AttributeFactory = () => method.GetCustomAttributes().ToArray(),
            DataSources = [],
            ClassDataSources = [],
            PropertyDataSources = []
        };
    }

    private static TestMetadata CreateFailedTestMetadataForDynamicTest(DynamicDiscoveryResult result, Exception ex)
    {
        var testName = $"[DYNAMIC TEST FAILED] {result.TestClassType?.Name ?? "Unknown"}";
        var displayName = $"{testName} - {ex.Message}";

        return new FailedTestMetadata(ex, displayName)
        {
            TestName = testName,
            TestClassType = result.TestClassType ?? typeof(object),
            TestMethodName = "DynamicTestFailed",
            FilePath = result.CreatorFilePath ?? "Unknown",
            LineNumber = result.CreatorLineNumber ?? 0,
            MethodMetadata = CreateDummyMethodMetadata(result.TestClassType ?? typeof(object), "DynamicTestFailed"),
            AttributeFactory = () => result.Attributes?.ToArray() ?? [],
            DataSources = [],
            ClassDataSources = [],
            PropertyDataSources = []
        };
    }

    private sealed class DynamicReflectionTestMetadata : TestMetadata, IDynamicTestMetadata
    {
        private readonly DynamicDiscoveryResult _dynamicResult;
        private readonly Type _testClass;
        private readonly MethodInfo _testMethod;

        public DynamicReflectionTestMetadata(Type testClass, MethodInfo testMethod, DynamicDiscoveryResult dynamicResult)
        {
            _testClass = testClass;
            _testMethod = testMethod;
            _dynamicResult = dynamicResult;
        }

        public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
        {
            get => (context, metadata) =>
            {
                // For dynamic tests, we need to use the specific arguments from the dynamic result
                var modifiedContext = new ExecutableTestCreationContext
                {
                    TestId = context.TestId,
                    DisplayName = context.DisplayName,
                    Arguments = _dynamicResult.TestMethodArguments ?? context.Arguments,
                    ClassArguments = _dynamicResult.TestClassArguments ?? context.ClassArguments,
                    Context = context.Context
                };

                // Create a regular ExecutableTest with the modified context
                // Create instance and test invoker for the dynamic test
                Func<TestContext, Task<object>> createInstance = async (TestContext testContext) =>
                {
                    // Try to create instance with ClassConstructor attribute
                    var attributes = metadata.AttributeFactory();
                    var classConstructorInstance = await ClassConstructorHelper.TryCreateInstanceWithClassConstructor(
                        attributes,
                        _testClass,
                        metadata.TestSessionId,
                        testContext).ConfigureAwait(false);

                    if (classConstructorInstance != null)
                    {
                        return classConstructorInstance;
                    }

                    // Fall back to default instance factory
                    var instance = metadata.InstanceFactory(Type.EmptyTypes, modifiedContext.ClassArguments);

                    // Handle property injections
                    foreach (var propertyInjection in metadata.PropertyInjections)
                    {
                        var value = propertyInjection.ValueFactory();
                        propertyInjection.Setter(instance, value);
                    }

                    return instance;
                };

                var invokeTest = metadata.TestInvoker ?? throw new InvalidOperationException("Test invoker is null");

                return new ExecutableTest(createInstance,
                    async (instance, args, context, ct) => await invokeTest(instance, args).ConfigureAwait(false))
                {
                    TestId = modifiedContext.TestId,
                    Metadata = metadata,
                    Arguments = modifiedContext.Arguments,
                    ClassArguments = modifiedContext.ClassArguments,
                    Context = modifiedContext.Context
                };
            };
        }
    }

}
