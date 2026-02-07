using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Discovery;

/// Discovers tests at runtime using reflection with assembly scanning and caching
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2062", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2111:Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can\'t guarantee availability of the requirements of the method.")]
[UnconditionalSuppressMessage("AOT", "IL3000", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode isn't used in AOT scenarios")]
[UnconditionalSuppressMessage("Trimming", "IL2055:Either the type on which the MakeGenericType is called can\'t be statically determined, or the type parameters to be used for generic arguments can\'t be statically determined.")]
[UnconditionalSuppressMessage("Trimming", "IL2060:Call to \'System.Reflection.MethodInfo.MakeGenericMethod\' can not be statically analyzed. It\'s not possible to guarantee the availability of requirements of the generic method.")]
internal sealed class ReflectionTestDataCollector : ITestDataCollector
{
    private static readonly ConcurrentDictionary<Assembly, bool> _scannedAssemblies = new();
    private static ImmutableList<TestMetadata> _discoveredTests = ImmutableList<TestMetadata>.Empty;
    private static readonly ConcurrentDictionary<Assembly, Type[]> _assemblyTypesCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo[]> _typeMethodsCache = new();

    private static Assembly[] Assemblies => field ??= FindAssemblies();

    private static Assembly[] FindAssemblies()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        if (assemblies.Length == 0)
        {
            return [ Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ];
        }

        return  assemblies;
    }

    private async Task<List<TestMetadata>> ProcessAssemblyAsync(Assembly assembly, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_scannedAssemblies.TryAdd(assembly, true))
            {
                return [];
            }

            try
            {
                return await DiscoverTestsInAssembly(assembly).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Create a failed test metadata for the assembly that couldn't be scanned
                var failedTest = CreateFailedTestMetadataForAssembly(assembly, ex);
                return [failedTest];
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new Exception("Using TUnit Reflection mechanisms isn't supported in AOT mode");
        }
#endif

        var allAssemblies = Assemblies;

        var assembliesList = new List<Assembly>(allAssemblies.Length);
        foreach (var assembly in allAssemblies)
        {
            if (ShouldScanAssembly(assembly))
            {
                assembliesList.Add(assembly);
            }
        }
        var assemblies = assembliesList;

        var maxConcurrency = Math.Min(assemblies.Count, Environment.ProcessorCount * 2);
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new Task<List<TestMetadata>>[assemblies.Count];

        for (var i = 0; i < assemblies.Count; i++)
        {
            var assembly = assemblies[i];
            var index = i;

            tasks[index] = ProcessAssemblyAsync(assembly, semaphore);
        }

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var totalCount = results.Sum(r => r.Count);
        var newTests = new List<TestMetadata>(totalCount);
        foreach (var tests in results)
        {
            newTests.AddRange(tests);
        }

        // Discover dynamic tests from DynamicTestBuilderAttribute methods
        var dynamicTests = await DiscoverDynamicTests(testSessionId).ConfigureAwait(false);
        newTests.AddRange(dynamicTests);

        // Atomic swap - no lock needed for readers
        ImmutableList<TestMetadata> original, updated;
        do
        {
            original = _discoveredTests;
            updated = original.AddRange(newTests);
        } while (Interlocked.CompareExchange(ref _discoveredTests, updated, original) != original);

        return _discoveredTests;
    }

    public Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId, ITestExecutionFilter? filter)
    {
        // Reflection mode doesn't benefit from filter pre-filtering (no type-indexed sources)
        // Delegate to the standard collection method
        return CollectTestsAsync(testSessionId);
    }

    public async IAsyncEnumerable<TestMetadata> CollectTestsStreamingAsync(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get assemblies to scan
        var allAssemblies = Assemblies;
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
                ImmutableInterlocked.Update(ref _discoveredTests, static (list, test) => list.Add(test), test);
                yield return test;
            }
        }

        // Stream dynamic tests
        await foreach (var dynamicTest in DiscoverDynamicTestsStreamingAsync(testSessionId, cancellationToken))
        {
            ImmutableInterlocked.Update(ref _discoveredTests, static (list, dynamicTest) => list.Add(dynamicTest), dynamicTest);
            yield return dynamicTest;
        }
    }

    private static IEnumerable<MethodInfo> GetAllTestMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        return _typeMethodsCache.GetOrAdd(type, static t =>
        {
            var methods = new List<MethodInfo>(20);
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
            var location = assembly.Location;
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

        var referencedAssemblies = AssemblyReferenceCache.GetReferencedAssemblies(assembly);
        var hasTUnitReference = false;
        foreach (var reference in referencedAssemblies)
        {
            if (reference.Name != null && (reference.Name.StartsWith("TUnit") || reference.Name == "TUnit"))
            {
                hasTUnitReference = true;
                break;
            }
        }

        if (!hasTUnitReference)
        {
            return false;
        }

        return true;
    }

    private static async Task<List<TestMetadata>> DiscoverTestsInAssembly(Assembly assembly)
    {
        var discoveredTests = new List<TestMetadata>(100);

        var types = _assemblyTypesCache.GetOrAdd(assembly, asm =>
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException reflectionTypeLoadException)
            {
                return reflectionTypeLoadException.Types.Where(static x => x != null).ToArray()!;
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

        var filteredTypes = types.Where(static t => t.IsClass && !IsCompilerGenerated(t));

        foreach (var type in filteredTypes)
        {
            if (type.IsAbstract)
            {
                continue;
            }

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
                // Resolve method-level generic instantiations
                foreach (var (resolvedMethod, failedMetadata) in ResolveMethodInstantiations(type, method))
                {
                    if (failedMetadata != null)
                    {
                        discoveredTests.Add(failedMetadata);
                        continue;
                    }

                    try
                    {
                        var metadata = await BuildTestMetadata(type, resolvedMethod).ConfigureAwait(false);
                        discoveredTests.Add(metadata);
                    }
                    catch (Exception ex)
                    {
                        var failedTest = CreateFailedTestMetadata(type, resolvedMethod, ex);
                        discoveredTests.Add(failedTest);
                    }
                }
            }
        }

        return discoveredTests;
    }

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
                if (loadedTypes == null)
                {
                    return [];
                }

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

        var filteredTypes = types.Where(static t => t.IsClass && !IsCompilerGenerated(t));

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

            IEnumerable<MethodInfo> testMethods;
            try
            {
                // Check if this class inherits tests from base classes
                var inheritsTests = type.IsDefined(typeof(InheritsTestsAttribute), inherit: false);

                if (inheritsTests)
                {
                    // Get all test methods including inherited ones
                    testMethods = GetAllTestMethods(type)
                        .Where(static m => m.IsDefined(typeof(TestAttribute), inherit: false) && !m.IsAbstract);
                }
                else
                {
                    // Only get declared test methods
                    testMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                                  BindingFlags.DeclaredOnly)
                        .Where(static m => m.IsDefined(typeof(TestAttribute), inherit: false) && !m.IsAbstract);
                }
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var method in testMethods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Prevent duplicate test metadata for inherited tests
                if (method.DeclaringType != type && !type.IsDefined(typeof(InheritsTestsAttribute), inherit: false))
                {
                    continue;
                }

                // Resolve method-level generic instantiations
                foreach (var (resolvedMethod, methodFailedMetadata) in ResolveMethodInstantiations(type, method))
                {
                    if (methodFailedMetadata != null)
                    {
                        yield return methodFailedMetadata;
                        continue;
                    }

                    TestMetadata? testMetadata = null;
                    TestMetadata? failedMetadata = null;

                    try
                    {
                        testMetadata = await BuildTestMetadata(type, resolvedMethod).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Create a failed test metadata for discovery failures
                        failedMetadata = CreateFailedTestMetadata(type, resolvedMethod, ex);
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
    }

    private static async Task<List<TestMetadata>> DiscoverGenericTests(Type genericTypeDefinition)
    {
        var discoveredTests = new List<TestMetadata>(100);

        // Get test methods from the generic type definition first (needed for both data sources and GenerateGenericTest)
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

        // Check for GenerateGenericTest attributes that explicitly specify type arguments
        var generateGenericTestAttributes = genericTypeDefinition.GetCustomAttributes<GenerateGenericTestAttribute>(inherit: false).ToArray();
        foreach (var genAttr in generateGenericTestAttributes)
        {
            var typeArguments = genAttr.TypeArguments;
            if (typeArguments.Length == 0)
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
                        // Resolve method-level generic instantiations
                        foreach (var (resolvedMethod, methodFailedMetadata) in ResolveMethodInstantiations(concreteType, concreteMethod))
                        {
                            if (methodFailedMetadata != null)
                            {
                                discoveredTests.Add(methodFailedMetadata);
                                continue;
                            }

                            // Build test metadata for the concrete type
                            // No class data for GenerateGenericTest - it just provides type arguments
                            var testMetadata = await BuildTestMetadata(concreteType, resolvedMethod, null).ConfigureAwait(false);
                            discoveredTests.Add(testMetadata);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create concrete type for {genericTypeDefinition.FullName ?? genericTypeDefinition.Name} from [GenerateGenericTest]. " +
                    $"Error: {ex.Message}. " +
                    $"Generic parameter count: {genericTypeDefinition.GetGenericArguments().Length}, " +
                    $"Type arguments provided: {typeArguments.Length}", ex);
            }
        }

        // Extract class-level data sources that will determine the generic type arguments
        var classDataSources = ReflectionAttributeExtractor.ExtractDataSources(genericTypeDefinition);

        if (classDataSources.Length == 0)
        {
            // Return any tests discovered from GenerateGenericTest attributes
            return discoveredTests;
        }

        // For each data source combination, create a concrete generic type
        foreach (var dataSource in classDataSources)
        {
            var dataItems = await GetDataFromSourceAsync(dataSource, null!).ConfigureAwait(false);

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
                            // Resolve method-level generic instantiations
                            foreach (var (resolvedMethod, methodFailedMetadata) in ResolveMethodInstantiations(concreteType, concreteMethod))
                            {
                                if (methodFailedMetadata != null)
                                {
                                    discoveredTests.Add(methodFailedMetadata);
                                    continue;
                                }

                                // Build test metadata for the concrete type
                                // The concrete type already has its generic arguments resolved
                                // For generic types with primary constructors that were resolved from class-level data sources,
                                // we need to ensure the class data sources contain the specific data for this instantiation
                                var testMetadata = await BuildTestMetadata(concreteType, resolvedMethod, dataRow).ConfigureAwait(false);

                                discoveredTests.Add(testMetadata);
                            }
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

    private static async IAsyncEnumerable<TestMetadata> DiscoverGenericTestsStreamingAsync(
        Type genericTypeDefinition,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Get test methods from the generic type definition first (needed for both data sources and GenerateGenericTest)
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

        // Check for GenerateGenericTest attributes that explicitly specify type arguments
        var generateGenericTestAttributes = genericTypeDefinition.GetCustomAttributes<GenerateGenericTestAttribute>(inherit: false).ToArray();
        foreach (var genAttr in generateGenericTestAttributes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var typeArguments = genAttr.TypeArguments;
            if (typeArguments.Length == 0)
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
                        // Resolve method-level generic instantiations
                        foreach (var (resolvedMethod, methodFailedMetadata) in ResolveMethodInstantiations(concreteType, concreteMethod))
                        {
                            if (methodFailedMetadata != null)
                            {
                                successfulTests ??= [];
                                successfulTests.Add(methodFailedMetadata);
                                continue;
                            }

                            // Build test metadata for the concrete type
                            // No class data for GenerateGenericTest - it just provides type arguments
                            var testMetadata = await BuildTestMetadata(concreteType, resolvedMethod, null).ConfigureAwait(false);

                            successfulTests ??= [];
                            successfulTests.Add(testMetadata);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                failedMetadata = new FailedTestMetadata(
                    new InvalidOperationException(
                        $"Failed to create concrete type for {genericTypeDefinition.FullName ?? genericTypeDefinition.Name} from [GenerateGenericTest]. " +
                        $"Error: {ex.Message}. " +
                        $"Generic parameter count: {genericTypeDefinition.GetGenericArguments().Length}, " +
                        $"Type arguments: {string.Join(", ", typeArguments.Select(static t => t.Name))}", ex),
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

            if (failedMetadata != null)
            {
                yield return failedMetadata;
            }
            else if (successfulTests != null)
            {
                foreach (var test in successfulTests)
                {
                    yield return test;
                }
            }
        }

        // Extract class-level data sources that will determine the generic type arguments
        var classDataSources = ReflectionAttributeExtractor.ExtractDataSources(genericTypeDefinition);

        if (classDataSources.Length == 0)
        {
            // GenerateGenericTest tests have already been yielded above
            yield break;
        }

        // For each data source combination, create a concrete generic type
        foreach (var dataSource in classDataSources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dataItems = await GetDataFromSourceAsync(dataSource, null!).ConfigureAwait(false);

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
                            // Resolve method-level generic instantiations
                            foreach (var (resolvedMethod, methodFailedMetadata) in ResolveMethodInstantiations(concreteType, concreteMethod))
                            {
                                if (methodFailedMetadata != null)
                                {
                                    successfulTests ??= [];
                                    successfulTests.Add(methodFailedMetadata);
                                    continue;
                                }

                                // Build test metadata for the concrete type
                                // The concrete type already has its generic arguments resolved
                                // For generic types with primary constructors that were resolved from class-level data sources,
                                // we need to ensure the class data sources contain the specific data for this instantiation
                                var testMetadata = await BuildTestMetadata(concreteType, resolvedMethod, dataRow).ConfigureAwait(false);

                                successfulTests ??= [];
                                successfulTests.Add(testMetadata);
                            }
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
                            $"Type arguments: {string.Join(", ", typeArguments?.Select(static t => t.Name) ?? [])}",
                            ex),
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
        var data = new List<object?[]>(16);

        try
        {
            // Use the centralized factory for generic type discovery
            // Provide an instance factory for reflection mode that can handle property injection
            var metadata = DataGeneratorMetadataCreator.CreateForGenericTypeDiscovery(
                dataSource,
                methodMetadata,
                ReflectionInstanceFactory.CreateInstanceWithPropertyInjectionAsync);

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
        var depth = 0;
        var currentType = testClass.BaseType;

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

    private static Task<TestMetadata> BuildTestMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type testClass,
        MethodInfo testMethod,
        object?[]? classData = null)
    {
        var testName = GenerateTestName(testClass, testMethod);

        var inheritanceDepth = CalculateInheritanceDepth(testClass, testMethod);

        // Determine the actual class type for generic type resolution
        // If the method is declared in a generic base class, use the constructed version from the inheritance hierarchy
        var typeForGenericResolution = testClass;
        if (testMethod.DeclaringType != null &&
            testMethod.DeclaringType != testClass &&
            testMethod.DeclaringType.IsGenericTypeDefinition)
        {
            // Find the constructed generic type in the inheritance chain
            var baseType = testClass.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType &&
                    baseType.GetGenericTypeDefinition() == testMethod.DeclaringType)
                {
                    typeForGenericResolution = baseType;
                    break;
                }
                baseType = baseType.BaseType;
            }
        }

        try
        {
            return Task.FromResult<TestMetadata>(new ReflectionTestMetadata(testClass, testMethod)
            {
                TestName = testName,
                TestClassType = typeForGenericResolution, // Use resolved type for generic resolution (may be constructed generic base)
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
                GenericTypeInfo = ReflectionGenericTypeResolver.ExtractGenericTypeInfo(typeForGenericResolution),
                GenericMethodInfo = ReflectionGenericTypeResolver.ExtractGenericMethodInfo(testMethod),
                GenericMethodTypeArguments = testMethod.IsGenericMethodDefinition ? null : testMethod.GetGenericArguments(),
                AttributeFactory = () => ReflectionAttributeExtractor.GetAllAttributes(testClass, testMethod),
                RepeatCount = testMethod.GetCustomAttribute<RepeatAttribute>()?.Times
                    ?? testClass.GetCustomAttribute<RepeatAttribute>()?.Times,
                PropertyInjections = PropertySourceRegistry.DiscoverInjectableProperties(testClass),
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

        // For constructed generic methods (e.g., from MakeGenericMethod), include type arguments
        // This matches source-gen behavior where test names include type args like "MethodName<int>"
        if (testMethod.IsGenericMethod && !testMethod.IsGenericMethodDefinition)
        {
            var typeArgs = testMethod.GetGenericArguments();
            var typeArgNames = string.Join(", ", typeArgs.Select(static t => t.Name));
            return $"{testMethod.Name}<{typeArgNames}>";
        }

        // Default format - just method name
        return testMethod.Name;
    }

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

            var constructedTypeCtor = constructedTypeConstructors.FirstOrDefault(static c => c.GetParameters().Length == 0) ?? constructedTypeConstructors.First();
            var constructedTypeFactory = CreateReflectionInstanceFactory(constructedTypeCtor);

            // Return a factory that ignores type arguments since the type is already closed
            return (_, args) => constructedTypeFactory(args);
        }

        var constructors = testClass.GetConstructors();

        if (constructors.Length == 0)
        {
            return (_, _) => Activator.CreateInstance(testClass)!;
        }

        var ctor = constructors.FirstOrDefault(static c => c.GetParameters().Length == 0) ?? constructors.First();

        var factory = CreateReflectionInstanceFactory(ctor);
        return (_, args) => factory(args);
    }

    private static Func<object, object?[], Task> CreateTestInvoker(Type testClass, MethodInfo testMethod)
    {
        return CreateReflectionTestInvoker(testClass, testMethod);
    }

    private static bool IsCompilerGenerated([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
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

    private static bool HasTestMethods([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
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

    /// <summary>
    /// Resolves method-level generic instantiations from [GenerateGenericTest] attributes.
    /// For non-generic methods, yields the method as-is.
    /// For generic methods with [GenerateGenericTest], yields concrete method instantiations.
    /// </summary>
    private static IEnumerable<(MethodInfo Method, TestMetadata? FailedMetadata)> ResolveMethodInstantiations(
        Type concreteClassType,
        MethodInfo method)
    {
        // If method is not generic, yield it directly
        if (!method.IsGenericMethodDefinition)
        {
            yield return (method, null);
            yield break;
        }

        // Check for [GenerateGenericTest] attributes on the method
        var generateGenericTestAttributes = method.GetCustomAttributes<GenerateGenericTestAttribute>(inherit: false).ToArray();

        if (generateGenericTestAttributes.Length == 0)
        {
            // No [GenerateGenericTest] attributes - yield the generic method definition as-is
            // TestBuilder will attempt to infer types from data sources later
            yield return (method, null);
            yield break;
        }

        var methodGenericParams = method.GetGenericArguments();

        // Process each [GenerateGenericTest] attribute
        foreach (var genAttr in generateGenericTestAttributes)
        {
            var typeArguments = genAttr.TypeArguments;

            // Validate type argument count
            if (typeArguments.Length == 0)
            {
                var failedMetadata = CreateFailedMethodGenericMetadata(
                    concreteClassType,
                    method,
                    $"[GenerateGenericTest] on method '{method.Name}' has no type arguments");
                yield return (method, failedMetadata);
                continue;
            }

            if (typeArguments.Length != methodGenericParams.Length)
            {
                var failedMetadata = CreateFailedMethodGenericMetadata(
                    concreteClassType,
                    method,
                    $"[GenerateGenericTest] on method '{method.Name}' provides {typeArguments.Length} type argument(s) " +
                    $"but method requires {methodGenericParams.Length}. " +
                    $"Provided: [{string.Join(", ", typeArguments.Select(static t => t.Name))}]");
                yield return (method, failedMetadata);
                continue;
            }

            // Try to create concrete method - capture result outside try-catch for yield
            MethodInfo? resolvedMethod = null;
            TestMetadata? errorMetadata = null;

            try
            {
                // Create concrete method
                resolvedMethod = method.MakeGenericMethod(typeArguments);
            }
            catch (ArgumentException ex)
            {
                // Constraint violation
                errorMetadata = CreateFailedMethodGenericMetadata(
                    concreteClassType,
                    method,
                    $"[GenerateGenericTest] constraint violation on method '{method.Name}': {ex.Message}. " +
                    $"Type arguments: [{string.Join(", ", typeArguments.Select(static t => t.Name))}]");
            }
            catch (Exception ex)
            {
                errorMetadata = CreateFailedMethodGenericMetadata(
                    concreteClassType,
                    method,
                    $"Failed to create concrete method for '{method.Name}': {ex.Message}");
            }

            // Yield result outside of try-catch
            if (errorMetadata != null)
            {
                yield return (method, errorMetadata);
            }
            else if (resolvedMethod != null)
            {
                yield return (resolvedMethod, null);
            }
        }
    }

    /// <summary>
    /// Creates a failed test metadata for method-level generic resolution errors.
    /// </summary>
    private static TestMetadata CreateFailedMethodGenericMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type,
        MethodInfo method,
        string errorMessage)
    {
        var testName = $"[GENERIC METHOD RESOLUTION FAILED] {type.FullName}.{method.Name}";
        var displayName = $"{testName} - {errorMessage}";
        var exception = new InvalidOperationException(errorMessage);

        return new FailedTestMetadata(exception, displayName)
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

    private static string? ExtractFilePath(MethodInfo method)
    {
        return method.GetCustomAttribute<TestAttribute>()?.File;
    }

    private static int? ExtractLineNumber(MethodInfo method)
    {
        return method.GetCustomAttribute<TestAttribute>()?.Line;
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
            AttributeFactory = static () =>
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

    private static TestMetadata CreateFailedTestMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type,
        MethodInfo method,
        Exception ex)
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


    private static MethodMetadata CreateDummyMethodMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type,
        string methodName)
    {
        return new MethodMetadata
        {
            Name = methodName,
            Type = type,
            Class = new ClassMetadata
            {
                Name = type.Name,
                Type = type,
                TypeInfo = new ConcreteType(type),
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
            ReturnTypeInfo = new ConcreteType(typeof(void)),
            ReturnType = typeof(void),
            TypeInfo = new ConcreteType(type)
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

                for (var i = 0; i < parameters.Length && i < args.Length; i++)
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
    private static void InferGenericTypeMapping(Type paramType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type argType, Dictionary<Type, Type> typeMapping)
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

                for (var i = 0; i < paramGenArgs.Length && i < argGenArgs.Length; i++)
                {
                    InferGenericTypeMapping(paramGenArgs[i], argGenArgs[i], typeMapping);
                }
            }
            else
            {
                foreach (var iface in AssemblyReferenceCache.GetInterfaces(argType))
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == paramGenDef)
                    {
                        var paramGenArgs = paramType.GetGenericArguments();
                        var ifaceGenArgs = iface.GetGenericArguments();

                        for (var i = 0; i < paramGenArgs.Length && i < ifaceGenArgs.Length; i++)
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

            foreach (var iface in AssemblyReferenceCache.GetInterfaces(argType))
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == paramGenDef)
                {
                    var paramGenArgs = paramType.GetGenericArguments();
                    var ifaceGenArgs = iface.GetGenericArguments();

                    for (var i = 0; i < paramGenArgs.Length && i < ifaceGenArgs.Length; i++)
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
    private static bool IsCovariantCompatible(Type paramType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type argType)
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

        var argInterfaces = AssemblyReferenceCache.GetInterfaces(argType);
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
                    for (var j = 0; j < methodParams.Length && j < args.Length; j++)
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
                    for (var i = 0; i < genericParams.Length; i++)
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

                    // Check if we have a pre-compiled method for these type arguments (AOT-friendly)
                    var compiledMethod = Core.AotCompatibility.GenericTestRegistry.GetCompiledMethod(
                        testClass, testMethod.Name, typeArguments);

                    if (compiledMethod != null)
                    {
                        methodToInvoke = compiledMethod;
                    }
                    else
                    {
                        methodToInvoke = testMethod.MakeGenericMethod(typeArguments);

                        // Pre-JIT the constructed generic method on first invocation
                        RuntimeHelpers.PrepareMethod(methodToInvoke.MethodHandle);
                    }
                }

                // Cast arguments to the expected parameter types
                var parameters = methodToInvoke.GetParameters();
                var castedArgs = new object?[parameters.Length];

                // Check if the last parameter is a params array
                var lastParam = parameters.Length > 0 ? parameters[^1] : null;
                var isParamsArray = lastParam != null && lastParam.IsDefined(typeof(ParamArrayAttribute), false);

                if (isParamsArray && lastParam != null)
                {
                    // Handle params array parameter
                    var paramsElementType = lastParam.ParameterType.GetElementType();
                    var regularParamsCount = parameters.Length - 1;

                    // Process regular parameters first
                    for (var i = 0; i < regularParamsCount && i < args.Length; i++)
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

                    // Collect remaining arguments into params array
                    var paramsStartIndex = regularParamsCount;
                    var paramsCount = Math.Max(0, args.Length - paramsStartIndex);

                    if (paramsElementType != null)
                    {
                        // C# params semantics:
                        // - If single arg is null, pass null (not array with null element)
                        // - If single arg is already the correct array type, pass it directly
                        // - Otherwise, create array from remaining arguments
                        if (paramsCount == 1)
                        {
                            var singleArg = args[paramsStartIndex];
                            if (singleArg == null)
                            {
                                // Null should be passed as null, not as array with null element
                                castedArgs[regularParamsCount] = null;
                            }
                            else if (lastParam.ParameterType.IsAssignableFrom(singleArg.GetType()))
                            {
                                // If the argument is already the correct array type, use it directly
                                castedArgs[regularParamsCount] = singleArg;
                            }
                            else
                            {
                                // Single non-array argument - wrap in array
                                var singleElementArray = Array.CreateInstance(paramsElementType, 1);
                                if (paramsElementType.IsAssignableFrom(singleArg.GetType()))
                                {
                                    singleElementArray.SetValue(singleArg, 0);
                                }
                                else if (IsCovariantCompatible(paramsElementType, singleArg.GetType()))
                                {
                                    singleElementArray.SetValue(singleArg, 0);
                                }
                                else
                                {
                                    singleElementArray.SetValue(CastHelper.Cast(paramsElementType, singleArg), 0);
                                }
                                castedArgs[regularParamsCount] = singleElementArray;
                            }
                        }
                        else
                        {
                            // Multiple arguments or no arguments - create array
                            var paramsArray = Array.CreateInstance(paramsElementType, paramsCount);
                            for (var i = 0; i < paramsCount; i++)
                            {
                                var arg = args[paramsStartIndex + i];
                                if (arg != null)
                                {
                                    var argType = arg.GetType();
                                    if (paramsElementType.IsAssignableFrom(argType))
                                    {
                                        paramsArray.SetValue(arg, i);
                                    }
                                    else if (IsCovariantCompatible(paramsElementType, argType))
                                    {
                                        paramsArray.SetValue(arg, i);
                                    }
                                    else
                                    {
                                        paramsArray.SetValue(CastHelper.Cast(paramsElementType, arg), i);
                                    }
                                }
                                else
                                {
                                    paramsArray.SetValue(null, i);
                                }
                            }
                            castedArgs[regularParamsCount] = paramsArray;
                        }
                    }
                }
                else
                {
                    // Normal parameter handling when no params array
                    for (var i = 0; i < parameters.Length && i < args.Length; i++)
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
                // F# Async support (reuses existing AsyncConvert logic)
                return AsyncConvert.ConvertObject(result).AsTask();
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
        var dynamicTests = new List<TestMetadata>(50);

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

        var allAssemblies = Assemblies;
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

            foreach (var type in types)
            {
                if (!type.IsClass || IsCompilerGenerated(type))
                    continue;
                var declaredMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                var methodsList = new List<MethodInfo>(4);
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
        var allAssemblies = Assemblies;
        var assemblies = new List<Assembly>(allAssemblies.Length);
        foreach (var assembly in allAssemblies)
        {
            if (ShouldScanAssembly(assembly))
            {
                assemblies.Add(assembly);
            }
        }

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

            foreach (var type in types)
            {
                if (!type.IsClass || IsCompilerGenerated(type))
                    continue;
                var declaredMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                var methodsList = new List<MethodInfo>(4);
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
        var dynamicTests = new List<TestMetadata>(50);

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

    private async Task<List<TestMetadata>> ConvertDynamicTestToMetadata(AbstractDynamicTest abstractDynamicTest)
    {
        var testMetadataList = new List<TestMetadata>();

        foreach (var discoveryResult in abstractDynamicTest.GetTests())
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
            foreach (var discoveryResult in context.Tests.SelectMany(static t => t.GetTests()))
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

        var metadata = new DynamicTestMetadata(result)
        {
            TestName = testName,
            TestClassType = result.TestClassType,
            TestMethodName = methodInfo.Name,
            Dependencies = result.Attributes.OfType<DependsOnAttribute>().Select(static a => a.ToTestDependency()).ToArray(),
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
            AttributeFactory = () => GetDynamicTestAttributes(result),
            PropertyInjections = PropertySourceRegistry.DiscoverInjectableProperties(result.TestClassType)
        };

        return Task.FromResult<TestMetadata>(metadata);
    }

    private static Attribute[] GetDynamicTestAttributes(DynamicDiscoveryResult result)
    {
        if (result.TestClassType == null)
        {
            return result.Attributes.ToArray();
        }

        // Merge explicitly provided attributes with inherited class/assembly attributes
        // Order matches GetAllAttributes: method-level first (explicit), then class, then assembly
        var attributes = new List<Attribute>(result.Attributes);

        attributes.AddRange(result.TestClassType.GetCustomAttributes());
        attributes.AddRange(result.TestClassType.Assembly.GetCustomAttributes());

        return attributes.ToArray();
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

                // Extract method info from the expression
                var lambdaExpression = result.TestMethod as System.Linq.Expressions.LambdaExpression;
                if (lambdaExpression == null)
                {
                    throw new InvalidOperationException("Dynamic test method must be a lambda expression");
                }

                MethodInfo? methodInfo = null;
                if (lambdaExpression.Body is System.Linq.Expressions.MethodCallExpression methodCall)
                {
                    methodInfo = methodCall.Method;
                }
                else if (lambdaExpression.Body is System.Linq.Expressions.UnaryExpression { Operand: System.Linq.Expressions.MethodCallExpression unaryMethodCall })
                {
                    methodInfo = unaryMethodCall.Method;
                }

                if (methodInfo == null)
                {
                    throw new InvalidOperationException("Could not extract method info from dynamic test expression");
                }

                var testInstance = instance ?? throw new InvalidOperationException("Test instance is null");

                // Use the provided args from TestMethodArguments instead of the expression's placeholder values
                var invokeResult = methodInfo.Invoke(testInstance, args);

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
            AttributeFactory = static () => [],
            DataSources = [],
            ClassDataSources = [],
            PropertyDataSources = []
        };
    }

    private static TestMetadata CreateFailedTestMetadataForDynamicBuilder(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type,
        MethodInfo method,
        Exception ex)
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

    /// <summary>
    /// Enumerates lightweight test descriptors for fast filtering.
    /// In reflection mode, this scans assemblies to create descriptors without full materialization.
    /// Note: Reflection mode is less optimized than source-gen mode since we can't pre-compute filter hints.
    /// </summary>
    public IEnumerable<TestDescriptor> EnumerateDescriptors()
    {
        // In reflection mode, we don't have pre-computed descriptors like source-gen mode.
        // We enumerate types and methods to create descriptors, but can't extract
        // filter hints (categories, properties) without instantiating attributes.
        // This provides the structure for two-phase discovery but with limited optimization.

        var allAssemblies = Assemblies;
        foreach (var assembly in allAssemblies)
        {
            if (!ShouldScanAssembly(assembly))
            {
                continue;
            }

            var types = _assemblyTypesCache.GetOrAdd(assembly, asm =>
            {
                try
                {
                    return asm.GetTypes();
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    return rtle.Types.Where(static x => x != null).ToArray()!;
                }
                catch
                {
                    return [];
                }
            });

            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract || IsCompilerGenerated(type))
                {
                    continue;
                }

                // Skip generic type definitions for now - they're handled separately
                if (type.IsGenericTypeDefinition)
                {
                    continue;
                }

                MethodInfo[] testMethods;
                try
                {
                    var inheritsTests = type.IsDefined(typeof(InheritsTestsAttribute), inherit: false);
                    if (inheritsTests)
                    {
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
                catch
                {
                    continue;
                }

                foreach (var method in testMethods)
                {
                    // Create a descriptor for each test method
                    // In reflection mode, we can't pre-extract filter hints without instantiating attributes
                    var capturedType = type;
                    var capturedMethod = method;

                    var nestedClassName = TUnit.Core.Extensions.TestContextExtensions.GetNestedTypeName(type);
                    yield return new TestDescriptor
                    {
                        TestId = $"{type.FullName}.{method.Name}",
                        ClassName = nestedClassName,
                        MethodName = method.Name,
                        FullyQualifiedName = $"{type.FullName}.{method.Name}",
                        FilePath = ExtractFilePath(method) ?? "Unknown",
                        LineNumber = ExtractLineNumber(method) ?? 0,
                        Categories = [], // Can't extract without attribute instantiation
                        Properties = [], // Can't extract without attribute instantiation
                        HasDataSource = method.IsDefined(typeof(IDataSourceAttribute), inherit: false) ||
                                       type.IsDefined(typeof(IDataSourceAttribute), inherit: false),
                        RepeatCount = 0, // Can't extract without attribute instantiation
                        Materializer = CreateReflectionMaterializer(capturedType, capturedMethod)
                    };
                }
            }
        }
    }

    private static Func<string, CancellationToken, IAsyncEnumerable<TestMetadata>> CreateReflectionMaterializer(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type,
        MethodInfo method)
    {
        return (testSessionId, cancellationToken) => MaterializeSingleTestAsync(type, method, cancellationToken);
    }

    private static async IAsyncEnumerable<TestMetadata> MaterializeSingleTestAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type,
        MethodInfo method,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TestMetadata metadata;
        try
        {
            metadata = await BuildTestMetadata(type, method).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            metadata = CreateFailedTestMetadata(type, method, ex);
        }

        yield return metadata;
    }

    /// <summary>
    /// Materializes full test metadata from filtered descriptors.
    /// Only called for tests that passed filtering.
    /// </summary>
    public async IAsyncEnumerable<TestMetadata> MaterializeFromDescriptorsAsync(
        IEnumerable<TestDescriptor> descriptors,
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var descriptor in descriptors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (var metadata in descriptor.Materializer(testSessionId, cancellationToken).ConfigureAwait(false))
            {
                yield return metadata;
            }
        }
    }

}
