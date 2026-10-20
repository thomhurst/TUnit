using System.Buffers;
using System.Collections.Concurrent;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
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
    // Per-session state: each TUnitServiceProvider instantiates its own collector, and these
    // fields must not leak between sessions. Under MTP server mode the test host can serve
    // many sessions sequentially or concurrently; static caches here would cause session N+1
    // to see session N's assemblies as "already scanned" and return zero tests (issue #6001).
    private readonly ConcurrentDictionary<Assembly, bool> _scannedAssemblies = new();
    private ImmutableList<TestMetadata> _discoveredTests = ImmutableList<TestMetadata>.Empty;

    // Process-wide caches: pure reflection metadata that doesn't change across sessions.
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

        var maxConcurrency = Math.Max(1, Math.Min(assemblies.Count, Environment.ProcessorCount * 2));
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

    // Read-many during reflection discovery (one Contains per assembly).
    // Frozen on net8+ for faster lookups.
#if NET8_0_OR_GREATER
    private static readonly System.Collections.Frozen.FrozenSet<string> ExcludedAssemblyNames =
        new HashSet<string>
        {
#else
    private static readonly HashSet<string> ExcludedAssemblyNames =
        new()
        {
#endif
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
        }
#if NET8_0_OR_GREATER
        .ToFrozenSet()
#endif
        ;

    // Matches whole directory names rather than substrings: a substring test on "ref" also
    // matched user paths such as "C:\src\reflection-tests" or "~/refactoring/", which
    // silently excluded every test assembly from reflection discovery.
    private static readonly string[] ExcludedLocationDirectoryNames =
    [
        "ref",
        "runtimes",
        "Microsoft.NETCore.App",
        "Microsoft.AspNetCore.App",
        "Microsoft.WindowsDesktop.App",
    ];

    internal static bool IsFrameworkOrReferenceAssemblyLocation(string location)
    {
        var directory = Path.GetDirectoryName(location);
        while (!string.IsNullOrEmpty(directory))
        {
            var directoryName = Path.GetFileName(directory);
            foreach (var excluded in ExcludedLocationDirectoryNames)
            {
                if (string.Equals(directoryName, excluded, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            directory = Path.GetDirectoryName(directory);
        }

        return false;
    }

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

        if (name.EndsWith(".resources", StringComparison.Ordinal) || name.EndsWith(".XmlSerializers", StringComparison.Ordinal))
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
            if (!string.IsNullOrEmpty(location) && IsFrameworkOrReferenceAssemblyLocation(location))
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
            if (reference.Name != null && (reference.Name.StartsWith("TUnit", StringComparison.Ordinal) || reference.Name == "TUnit"))
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

    /// <summary>
    /// Filters the non-null entries out of <see cref="ReflectionTypeLoadException.Types"/>, using a
    /// pooled scratch buffer to avoid the per-failure allocations of the old LINQ Where().ToArray().
    /// Shared by both the eager and streaming discovery type caches.
    /// </summary>
    private static Type[] FilterLoadedTypes(Type?[]? loadedTypes)
    {
        if (loadedTypes is null || loadedTypes.Length == 0)
        {
            return [];
        }

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

            if (validCount == 0)
            {
                return [];
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
                return FilterLoadedTypes(reflectionTypeLoadException.Types);
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

        foreach (var type in types)
        {
            if (!type.IsClass || IsCompilerGenerated(type))
            {
                continue;
            }

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
                foreach (var (resolvedMethod, failedMetadata, methodArguments) in ResolveMethodInstantiations(type, method))
                {
                    if (failedMetadata != null)
                    {
                        discoveredTests.Add(failedMetadata);
                        continue;
                    }

                    try
                    {
                        var metadata = BuildTestMetadata(type, resolvedMethod, methodDataSources: methodArguments);
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
                // Some types might fail to load, but we can still use the ones that loaded successfully.
                return FilterLoadedTypes(rtle.Types);
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

        foreach (var type in types)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!type.IsClass || IsCompilerGenerated(type))
            {
                continue;
            }

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
                    testMethods = GetAllTestMethods(type);
                }
                else
                {
                    // Only get declared test methods
                    testMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                                  BindingFlags.DeclaredOnly);
                }
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var method in testMethods)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!method.IsDefined(typeof(TestAttribute), inherit: false) || method.IsAbstract)
                {
                    continue;
                }

                // Prevent duplicate test metadata for inherited tests
                if (method.DeclaringType != type && !type.IsDefined(typeof(InheritsTestsAttribute), inherit: false))
                {
                    continue;
                }

                // Resolve method-level generic instantiations
                foreach (var (resolvedMethod, methodFailedMetadata, methodArguments) in ResolveMethodInstantiations(type, method))
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
                        testMetadata = BuildTestMetadata(type, resolvedMethod, methodDataSources: methodArguments);
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

        // Extract class-level data sources that will determine the generic type arguments
        var classDataSources = ReflectionAttributeExtractor.ExtractDataSources(genericTypeDefinition);

        // Type arguments that break the class's constraints become failed tests for that row or
        // type only, never a failure of the whole assembly scan (#7107).
        var constraintViolations = new ReflectionGenericConstraintViolations(genericTypeDefinition, testMethods);

        // Check for GenerateGenericTest attributes that explicitly specify type arguments
        var generateGenericTestAttributes = genericTypeDefinition.GetCustomAttributes<GenerateGenericTestAttribute>(inherit: false).ToArray();

        // Without class-level data sources, method-level [Arguments] rows may infer the class's type
        // arguments instead (#7106). They do so alongside [GenerateGenericTest], as in the source generator.
        Dictionary<MethodInfo, List<Type[]>>? methodRowTypeArguments = null;
        var methodRowTests = classDataSources.Length == 0
            ? BuildTestsFromMethodRows(genericTypeDefinition, testMethods, generateGenericTestAttributes, out methodRowTypeArguments)
            : null;

        foreach (var genAttr in generateGenericTestAttributes)
        {
            var typeArguments = genAttr.TypeArguments;
            if (typeArguments.Length == 0 || constraintViolations.TryAddClassViolation(typeArguments))
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
                    var concreteMethod = FindConcreteMethod(concreteType, genericMethod);

                    if (concreteMethod != null && !IsInferredFromMethodRows(methodRowTypeArguments, genericMethod, typeArguments))
                    {
                        // Resolve method-level generic instantiations
                        foreach (var (resolvedMethod, methodFailedMetadata, methodArguments) in ResolveMethodInstantiations(concreteType, concreteMethod))
                        {
                            if (methodFailedMetadata != null)
                            {
                                discoveredTests.Add(methodFailedMetadata);
                                continue;
                            }

                            // Build test metadata for the concrete type
                            // No class data for GenerateGenericTest - it just provides type arguments
                            var testMetadata = BuildTestMetadata(concreteType, resolvedMethod, null, methodArguments);
                            discoveredTests.Add(testMetadata);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                discoveredTests.Add(CreateGenericTypeCreationFailedMetadata(genericTypeDefinition, new InvalidOperationException(
                    $"Failed to create concrete type for {genericTypeDefinition.FullName ?? genericTypeDefinition.Name} from [GenerateGenericTest]. " +
                    $"Error: {ex.Message}. " +
                    $"Generic parameter count: {genericTypeDefinition.GetGenericArguments().Length}, " +
                    $"Type arguments provided: {typeArguments.Length}", ex)));
            }
        }

        if (methodRowTests != null)
        {
            discoveredTests.AddRange(methodRowTests);

            // Return any tests discovered from method rows and GenerateGenericTest attributes
            AddConstraintViolationTests(discoveredTests, constraintViolations);
            return discoveredTests;
        }

        // One concrete generic type per distinct set of inferred type arguments, carrying every class row
        // that inferred it, so rows sharing a type each still produce a test (#7106)
        foreach (var (typeArguments, classRowSources, rowCount) in await GroupClassRowsByTypeArgumentsAsync(genericTypeDefinition, classDataSources, constraintViolations, CancellationToken.None).ConfigureAwait(false))
        {
            try
            {
                // Create concrete type with validation
                var concreteType = ReflectionGenericTypeResolver.CreateConcreteType(genericTypeDefinition, typeArguments);

                // Build tests for each method in the concrete type
                foreach (var genericMethod in testMethods)
                {
                    var concreteMethod = FindConcreteMethod(concreteType, genericMethod);

                    if (concreteMethod != null)
                    {
                        // Resolve method-level generic instantiations
                        foreach (var (resolvedMethod, methodFailedMetadata, methodArguments) in ResolveMethodInstantiations(concreteType, concreteMethod))
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
                            var testMetadata = BuildTestMetadata(concreteType, resolvedMethod, classRowSources, methodArguments);

                            discoveredTests.Add(testMetadata);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                discoveredTests.Add(CreateGenericTypeCreationFailedMetadata(genericTypeDefinition, new InvalidOperationException(
                    $"Failed to create concrete type for {genericTypeDefinition.FullName ?? genericTypeDefinition.Name}. " +
                    $"Error: {ex.Message}. " +
                    $"Generic parameter count: {genericTypeDefinition.GetGenericArguments().Length}, " +
                    $"Type arguments provided: {typeArguments.Length}, " +
                    $"Data rows: {rowCount}", ex)));
            }
        }

        AddConstraintViolationTests(discoveredTests, constraintViolations);
        return discoveredTests;
    }

    private static void AddConstraintViolationTests(List<TestMetadata> discoveredTests, ReflectionGenericConstraintViolations constraintViolations)
    {
        constraintViolations.AddMethodRowViolations();
        discoveredTests.AddRange(constraintViolations.CreateMetadata());
    }

    private static TestMetadata CreateGenericTypeCreationFailedMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type genericTypeDefinition,
        Exception exception)
    {
        // Fails this type instantiation only, instead of the whole assembly scan.
        return new FailedTestMetadata(exception, $"[GENERIC TYPE CREATION FAILED] {genericTypeDefinition.Name}")
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

        // Extract class-level data sources that will determine the generic type arguments
        var classDataSources = ReflectionAttributeExtractor.ExtractDataSources(genericTypeDefinition);

        // Type arguments that break the class's constraints become failed tests for that row or
        // type only, never a failure of the whole assembly scan (#7107).
        var constraintViolations = new ReflectionGenericConstraintViolations(genericTypeDefinition, testMethods);

        // Check for GenerateGenericTest attributes that explicitly specify type arguments
        var generateGenericTestAttributes = genericTypeDefinition.GetCustomAttributes<GenerateGenericTestAttribute>(inherit: false).ToArray();

        // Without class-level data sources, method-level [Arguments] rows may infer the class's type
        // arguments instead (#7106). They do so alongside [GenerateGenericTest], as in the source generator.
        Dictionary<MethodInfo, List<Type[]>>? methodRowTypeArguments = null;
        var methodRowTests = classDataSources.Length == 0
            ? BuildTestsFromMethodRows(genericTypeDefinition, testMethods, generateGenericTestAttributes, out methodRowTypeArguments)
            : null;

        foreach (var genAttr in generateGenericTestAttributes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var typeArguments = genAttr.TypeArguments;
            if (typeArguments.Length == 0 || constraintViolations.TryAddClassViolation(typeArguments))
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
                    var concreteMethod = FindConcreteMethod(concreteType, genericMethod);

                    if (concreteMethod != null && !IsInferredFromMethodRows(methodRowTypeArguments, genericMethod, typeArguments))
                    {
                        // Resolve method-level generic instantiations
                        foreach (var (resolvedMethod, methodFailedMetadata, methodArguments) in ResolveMethodInstantiations(concreteType, concreteMethod))
                        {
                            if (methodFailedMetadata != null)
                            {
                                successfulTests ??= [];
                                successfulTests.Add(methodFailedMetadata);
                                continue;
                            }

                            // Build test metadata for the concrete type
                            // No class data for GenerateGenericTest - it just provides type arguments
                            var testMetadata = BuildTestMetadata(concreteType, resolvedMethod, null, methodArguments);

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

        if (methodRowTests != null)
        {
            foreach (var test in methodRowTests)
            {
                yield return test;
            }

            // GenerateGenericTest tests have already been yielded above
            constraintViolations.AddMethodRowViolations();
            foreach (var violationMetadata in constraintViolations.CreateMetadata())
            {
                yield return violationMetadata;
            }

            yield break;
        }

        // One concrete generic type per distinct set of inferred type arguments, carrying every class row
        // that inferred it, so rows sharing a type each still produce a test (#7106)
        foreach (var (typeArguments, classRowSources, _) in await GroupClassRowsByTypeArgumentsAsync(genericTypeDefinition, classDataSources, constraintViolations, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            TestMetadata? failedMetadata = null;
            List<TestMetadata>? successfulTests = null;

            try
            {
                // Create concrete type with validation
                var concreteType = ReflectionGenericTypeResolver.CreateConcreteType(genericTypeDefinition, typeArguments);

                // Build tests for each method in the concrete type
                foreach (var genericMethod in testMethods)
                {
                    var concreteMethod = FindConcreteMethod(concreteType, genericMethod);

                    if (concreteMethod != null)
                    {
                        // Resolve method-level generic instantiations
                        foreach (var (resolvedMethod, methodFailedMetadata, methodArguments) in ResolveMethodInstantiations(concreteType, concreteMethod))
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
                            var testMetadata = BuildTestMetadata(concreteType, resolvedMethod, classRowSources, methodArguments);

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
                        $"Type arguments: {string.Join(", ", typeArguments.Select(static t => t.Name))}",
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

        constraintViolations.AddMethodRowViolations();
        foreach (var violationMetadata in constraintViolations.CreateMetadata())
        {
            yield return violationMetadata;
        }
    }

    /// <summary>
    /// Evaluates the class-level data sources of a generic type definition and groups their rows by the
    /// type arguments each row infers. Each group keeps one static data source per original source, so
    /// every row runs as its own test on the shared concrete type, mirroring the source generator.
    /// Rows whose type arguments break the class's constraints go to <paramref name="constraintViolations"/>
    /// instead of a group (#7107).
    /// </summary>
    private static async Task<List<(Type[] TypeArguments, IDataSourceAttribute[] ClassDataSources, int RowCount)>> GroupClassRowsByTypeArgumentsAsync(
        Type genericTypeDefinition,
        IDataSourceAttribute[] classDataSources,
        ReflectionGenericConstraintViolations constraintViolations,
        CancellationToken cancellationToken)
    {
        var typeArgumentGroups = new List<Type[]>();
        var sourcesByGroup = new List<List<IDataSourceAttribute>>();
        var rowCounts = new List<int>();

        foreach (var dataSource in classDataSources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dataItems = await GetDataFromSourceAsync(dataSource, null!).ConfigureAwait(false);

            // Rows from this source, per group index, in row order
            var rowsByGroup = new Dictionary<int, List<object?[]>>();

            foreach (var dataRow in dataItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (dataRow == null || dataRow.Length == 0)
                {
                    continue;
                }

                // Determine generic type arguments from the data
                var typeArguments = ReflectionGenericTypeResolver.DetermineGenericTypeArguments(genericTypeDefinition, dataRow);
                if (typeArguments == null || typeArguments.Length == 0 || constraintViolations.TryAddClassViolation(typeArguments))
                {
                    continue;
                }

                var groupIndex = IndexOfTypeArguments(typeArgumentGroups, typeArguments);
                if (groupIndex < 0)
                {
                    groupIndex = typeArgumentGroups.Count;
                    typeArgumentGroups.Add(typeArguments);
                    sourcesByGroup.Add([]);
                    rowCounts.Add(0);
                }

                if (!rowsByGroup.TryGetValue(groupIndex, out var rows))
                {
                    rows = [];
                    rowsByGroup[groupIndex] = rows;
                }

                rows.Add(dataRow);
            }

            // Tests receive the rows evaluated here rather than the source itself, so the source is read
            // once and tests get the very objects discovery inferred their types from. The partition
            // records its source, which TestBuilder restores to the test's attributes, so per-row settings
            // ([Arguments(Skip/DisplayName/Categories)]) and registration callbacks still apply. It keeps the
            // source's DeferEnumeration, so discovery still shows one placeholder per instantiation and the
            // rows expand at runtime from this partition, without reading the source again.
            foreach (var entry in rowsByGroup)
            {
                sourcesByGroup[entry.Key].Add(new StaticDataSourceAttribute(entry.Value.ToArray())
                {
                    PartitionOf = dataSource,
                    DeferEnumeration = dataSource.DeferEnumeration
                });
                rowCounts[entry.Key] += entry.Value.Count;
            }
        }

        var result = new List<(Type[] TypeArguments, IDataSourceAttribute[] ClassDataSources, int RowCount)>(typeArgumentGroups.Count);
        for (var i = 0; i < typeArgumentGroups.Count; i++)
        {
            result.Add((typeArgumentGroups[i], sourcesByGroup[i].ToArray(), rowCounts[i]));
        }

        return result;
    }

    /// <summary>
    /// Finds the method on a constructed generic type that corresponds to a method of its generic type
    /// definition. Matches by metadata identity rather than name so overloaded test methods resolve to
    /// their own counterpart instead of throwing <see cref="AmbiguousMatchException"/>.
    /// </summary>
    private static MethodInfo? FindConcreteMethod(Type concreteType, MethodInfo genericMethod)
    {
        foreach (var method in concreteType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (method.MetadataToken == genericMethod.MetadataToken && method.Module == genericMethod.Module)
            {
                return method;
            }
        }

        return null;
    }

    private static int IndexOfTypeArguments(List<Type[]> typeArgumentGroups, Type[] typeArguments)
    {
        for (var i = 0; i < typeArgumentGroups.Count; i++)
        {
            if (AreSameTypeArguments(typeArgumentGroups[i], typeArguments))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool AreSameTypeArguments(Type[] left, Type[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// For a generic class without class-level data sources, infers the class's type arguments from the
    /// method-level [Arguments] rows of each non-generic test method whose parameters use them, and builds
    /// one test per inferred instantiation carrying only the rows that inferred it, mirroring the source
    /// generator (#7106). An instantiation that a [GenerateGenericTest] also lists stands for that attribute's
    /// instantiation too, so it carries every method data source except rows inferring other type arguments:
    /// rows that infer nothing, such as [Arguments(null)], still run on it.
    /// <paramref name="inferredTypeArguments"/> receives, per test method, every type argument combination
    /// its rows inferred, including combinations that violate the class's constraints.
    /// </summary>
    private static List<TestMetadata> BuildTestsFromMethodRows(Type genericTypeDefinition, MethodInfo[] testMethods, GenerateGenericTestAttribute[] generateGenericTestAttributes, out Dictionary<MethodInfo, List<Type[]>>? inferredTypeArguments)
    {
        var tests = new List<TestMetadata>();
        inferredTypeArguments = null;
        var classGenericParameters = genericTypeDefinition.GetGenericArguments();

        foreach (var genericMethod in testMethods)
        {
            if (genericMethod.IsGenericMethodDefinition)
            {
                continue;
            }

            var parameters = genericMethod.GetParameters();
            if (!Array.Exists(parameters, static p => p.ParameterType.ContainsGenericParameters))
            {
                continue;
            }

            var methodDataSources = ReflectionAttributeExtractor.ExtractDataSources(genericMethod);
            var typeArgumentGroups = new List<Type[]>();

            // The group whose type arguments each data source's row inferred, or -1 when it inferred none
            var groupOfSource = new int[methodDataSources.Length];

            for (var i = 0; i < methodDataSources.Length; i++)
            {
                groupOfSource[i] = -1;

                if (methodDataSources[i] is not ArgumentsAttribute arguments)
                {
                    continue;
                }

                var typeArguments = InferClassTypeArgumentsFromMethodRow(classGenericParameters, parameters, arguments);
                if (typeArguments == null)
                {
                    continue;
                }

                var groupIndex = IndexOfTypeArguments(typeArgumentGroups, typeArguments);
                if (groupIndex < 0)
                {
                    groupIndex = typeArgumentGroups.Count;
                    typeArgumentGroups.Add(typeArguments);
                }

                groupOfSource[i] = groupIndex;
            }

            if (typeArgumentGroups.Count > 0)
            {
                (inferredTypeArguments ??= new Dictionary<MethodInfo, List<Type[]>>())[genericMethod] = typeArgumentGroups;
            }

            for (var groupIndex = 0; groupIndex < typeArgumentGroups.Count; groupIndex++)
            {
                Type concreteType;
                try
                {
                    concreteType = ReflectionGenericTypeResolver.CreateConcreteType(genericTypeDefinition, typeArgumentGroups[groupIndex]);
                }
                catch (ArgumentException)
                {
                    // The inferred types violate the class's generic constraints. Those rows are reported
                    // as failed tests by ReflectionGenericConstraintViolations, as in the source generator (#7107).
                    continue;
                }
                catch (Exception ex)
                {
                    tests.Add(CreateFailedTestMetadata(genericTypeDefinition, genericMethod, ex));
                    continue;
                }

                try
                {
                    var concreteMethod = FindConcreteMethod(concreteType, genericMethod);
                    if (concreteMethod == null)
                    {
                        continue;
                    }

                    var isListedByGenerateGenericTest = IsListedByGenerateGenericTest(generateGenericTestAttributes, typeArgumentGroups[groupIndex]);

                    // Same attribute order as the generic definition's method, so the indices line up
                    var concreteDataSources = ReflectionAttributeExtractor.ExtractMethodDataSources(concreteMethod, concreteType);
                    var rows = new List<IDataSourceAttribute>(concreteDataSources.Length);
                    for (var i = 0; i < concreteDataSources.Length; i++)
                    {
                        if (groupOfSource[i] == groupIndex || isListedByGenerateGenericTest && groupOfSource[i] < 0)
                        {
                            rows.Add(concreteDataSources[i]);
                        }
                    }

                    tests.Add(BuildTestMetadata(concreteType, concreteMethod, classDataSources: [], methodDataSources: rows.ToArray()));
                }
                catch (Exception ex)
                {
                    tests.Add(CreateFailedTestMetadata(genericTypeDefinition, genericMethod, ex));
                }
            }
        }

        return tests;
    }

    /// <summary>
    /// True when a method's [Arguments] rows already infer the type arguments a [GenerateGenericTest]
    /// lists. The rows' instantiation, scoped to those rows, then stands for both, as in the source
    /// generator, which keeps the first instantiation of a type combination.
    /// </summary>
    private static bool IsListedByGenerateGenericTest(GenerateGenericTestAttribute[] generateGenericTestAttributes, Type[] typeArguments)
    {
        foreach (var attribute in generateGenericTestAttributes)
        {
            if (AreSameTypeArguments(attribute.TypeArguments, typeArguments))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInferredFromMethodRows(Dictionary<MethodInfo, List<Type[]>>? methodRowTypeArguments, MethodInfo genericMethod, Type[] typeArguments) =>
        methodRowTypeArguments != null
        && methodRowTypeArguments.TryGetValue(genericMethod, out var groups)
        && IndexOfTypeArguments(groups, typeArguments) >= 0;

    private static Type[]? InferClassTypeArgumentsFromMethodRow(Type[] classGenericParameters, ParameterInfo[] parameters, ArgumentsAttribute arguments)
    {
        // [Arguments(null)] (a null params array) infers no type, as in the source generator
        if (arguments.IsNullValuesArray)
        {
            return null;
        }

        var values = arguments.Values;
        var typeMapping = new Dictionary<Type, Type>();
        Dictionary<Type, Type>? parameterMapping = null;

        for (var i = 0; i < parameters.Length && i < values.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (!parameterType.ContainsGenericParameters)
            {
                continue;
            }

            // Match the source generator: Roslyn types a null element of the params object?[]
            // argument as object
            var argumentType = values[i] is { } value ? GetArgumentInferenceType(value) : typeof(object);

            // Also match its first-wins rule across parameters: the first parameter that uses a type
            // parameter fixes it, so Run(T first, T second) with [Arguments(null, 1)] infers T = object
            if (parameterType.IsGenericParameter)
            {
                if (!typeMapping.ContainsKey(parameterType))
                {
                    typeMapping[parameterType] = argumentType;
                }

                continue;
            }

            parameterMapping ??= new Dictionary<Type, Type>();
            parameterMapping.Clear();
            InferGenericTypeMapping(parameterType, argumentType, parameterMapping);

            foreach (var mapping in parameterMapping)
            {
                if (!typeMapping.ContainsKey(mapping.Key))
                {
                    typeMapping[mapping.Key] = mapping.Value;
                }
            }
        }

        var typeArguments = new Type[classGenericParameters.Length];
        for (var i = 0; i < classGenericParameters.Length; i++)
        {
            if (!typeMapping.TryGetValue(classGenericParameters[i], out var typeArgument))
            {
                return null;
            }

            typeArguments[i] = typeArgument;
        }

        return typeArguments;
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

    private static TestMetadata BuildTestMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type testClass,
        MethodInfo testMethod,
        IDataSourceAttribute[]? classDataSources = null,
        IDataSourceAttribute[]? methodDataSources = null)
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
            return new ReflectionTestMetadata(testClass, testMethod)
            {
                TestName = testName,
                TestClassType = typeForGenericResolution, // Use resolved type for generic resolution (may be constructed generic base)
                TestMethodName = testMethod.Name,
                Dependencies = ReflectionAttributeExtractor.ExtractDependencies(testClass, testMethod),
                DataSources = methodDataSources ?? ReflectionAttributeExtractor.ExtractMethodDataSources(testMethod, testClass),
                ClassDataSources = classDataSources ?? ReflectionAttributeExtractor.ExtractDataSources(testClass),
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
                    ?? testClass.GetCustomAttribute<RepeatAttribute>()?.Times
                    ?? testClass.Assembly.GetCustomAttribute<RepeatAttribute>()?.Times,
                PropertyInjections = PropertySourceRegistry.DiscoverInjectableProperties(testClass),
                InheritanceDepth = inheritanceDepth
            };
        }
        catch (Exception ex)
        {
            return CreateFailedTestMetadata(testClass, testMethod, ex);
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
        // formatted as source-gen formats them, like "MethodName<int>" (#7105)
        if (testMethod.IsGenericMethod && !testMethod.IsGenericMethodDefinition)
        {
            return TypeNameFormatter.AppendGenericArguments(testMethod.Name, testMethod.GetGenericArguments());
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
    /// For generic methods with [GenerateGenericTest], yields concrete method instantiations the way the
    /// source generator does. First, each data source that infers the type arguments closes the method over
    /// them (<c>[Arguments(1)]</c> becomes <c>Test&lt;int&gt;(1)</c>; see
    /// <see cref="InferMethodInstantiationsFromDataSources"/>). Then each [GenerateGenericTest] type set that
    /// no data source inferred runs with every data source.
    /// <c>MethodArguments</c> restricts the data sources for that instantiation; null means all of them.
    /// </summary>
    private static IEnumerable<(MethodInfo Method, TestMetadata? FailedMetadata, IDataSourceAttribute[]? MethodArguments)> ResolveMethodInstantiations(
        Type concreteClassType,
        MethodInfo method)
    {
        // If method is not generic, yield it directly
        if (!method.IsGenericMethodDefinition)
        {
            yield return (method, null, null);
            yield break;
        }

        // Check for [GenerateGenericTest] attributes on the method
        var generateGenericTestAttributes = method.GetCustomAttributes<GenerateGenericTestAttribute>(inherit: false).ToArray();

        if (generateGenericTestAttributes.Length == 0)
        {
            // No [GenerateGenericTest] attributes - yield the generic method definition as-is
            // TestBuilder will attempt to infer types from data sources later
            yield return (method, null, null);
            yield break;
        }

        var methodGenericParams = method.GetGenericArguments();
        var processedTypeArguments = new HashSet<Type[]>(TypeArrayComparer.Instance);

        foreach (var (inferredMethod, typeArguments, rows) in InferMethodInstantiationsFromDataSources(concreteClassType, method, methodGenericParams.Length))
        {
            processedTypeArguments.Add(typeArguments);
            yield return (inferredMethod, null, rows);
        }

        // [GenerateGenericTest] types that break the method's constraints. They are reported together
        // as one failed test per type, with the same message as source-generated mode (#7107).
        List<string>? constraintViolations = null;

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
                yield return (method, failedMetadata, null);
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
                yield return (method, failedMetadata, null);
                continue;
            }

            // A data source already produced this instantiation
            if (!processedTypeArguments.Add(typeArguments))
            {
                continue;
            }

            if (ReflectionGenericTypeResolver.FindConstraintViolation(method, typeArguments) is { } constraintViolation)
            {
                (constraintViolations ??= []).Add(constraintViolation);
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
                yield return (method, errorMetadata, null);
            }
            else if (resolvedMethod != null)
            {
                yield return (resolvedMethod, null, null);
            }
        }

        if (constraintViolations != null)
        {
            var testAttribute = method.GetCustomAttribute<TestAttribute>();
            yield return (method, GenericConstraintViolationTestMetadata.Create(
                concreteClassType,
                method.Name,
                ReflectionMetadataBuilder.CreateMethodMetadata(concreteClassType, method),
                testAttribute?.File ?? "Unknown",
                testAttribute?.Line ?? 0,
                constraintViolations), null);
        }
    }

    /// <summary>
    /// Closes <paramref name="method"/> over the types inferred from its data sources, in the order the source
    /// generator uses:
    /// <list type="number">
    /// <item>Each [Arguments] row (<c>InferTypesFromArgumentsAttribute</c>): a type parameter is inferred only
    /// from a value passed directly to a parameter of that type (a null value infers <c>object</c>), and every
    /// type parameter must be inferred. The instantiation runs only those rows.</item>
    /// <item>Each typed data source deriving from <c>DataSourceGeneratorAttribute&lt;T...&gt;</c> or
    /// <c>AsyncDataSourceGeneratorAttribute&lt;T...&gt;</c> (<c>InferTypesFromDataSourceAttribute</c>). The
    /// instantiation runs only that source.</item>
    /// <item>Parameter attributes implementing <see cref="TUnit.Core.Interfaces.IInfersType{T}"/>
    /// (<c>InferTypesFromTypeInferringAttributes</c>). That instantiation runs every data source, so its
    /// <c>Rows</c> is null.</item>
    /// <item>[MethodDataSource] members of the test class returning <c>IEnumerable&lt;Func&lt;(...)&gt;&gt;</c>
    /// (<c>InferTypesFromMethodDataSource</c>). The instantiation runs only that source.</item>
    /// </list>
    /// Sources whose types violate the constraints are skipped. Sources inferring types that are already
    /// instantiated are dropped, except further [Arguments] rows, which join the instantiation an earlier
    /// [Arguments] row created.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Reflection mode is not used in AOT; MakeGenericMethod over types inferred from attribute values")]
    private static List<(MethodInfo Method, Type[] TypeArguments, IDataSourceAttribute[]? Rows)> InferMethodInstantiationsFromDataSources(
        Type testClass,
        MethodInfo method,
        int genericParameterCount)
    {
        var instantiations = new List<(MethodInfo Method, Type[] TypeArguments, List<IDataSourceAttribute>? Rows)>();
        var parameters = method.GetParameters();
        var dataSources = ReflectionAttributeExtractor.ExtractDataSources(method);

        // Further [Arguments] rows inferring types that an earlier [Arguments] row instantiated join that
        // instantiation, as the generator does: on a non-generic class through isMethodArgumentsRow, and on a
        // generic class because an instantiation built from class and method rows runs every method [Arguments]
        // row. Any other source inferring types that are already instantiated is dropped, as the generator's
        // processedTypeCombinations does.
        foreach (var dataSource in dataSources)
        {
            if (dataSource is ArgumentsAttribute arguments)
            {
                AddInstantiation(InferMethodTypeArguments(arguments.Values, parameters, genericParameterCount), arguments, isMethodArgumentsRow: true);
            }
        }

        // On a generic class too (#7164), as the generator does
        var classGenericParameterCount = method.DeclaringType is { IsGenericType: true } declaringType
            ? declaringType.GetGenericArguments().Length
            : 0;
        foreach (var dataSource in dataSources)
        {
            if (dataSource is not ArgumentsAttribute)
            {
                AddInstantiation(InferTypesFromTypedDataSource(dataSource, genericParameterCount, classGenericParameterCount), dataSource);
            }
        }

        AddInstantiation(InferTypesFromTypeInferringAttributes(parameters, genericParameterCount), null);

        foreach (var dataSource in dataSources)
        {
            if (dataSource is MethodDataSourceAttribute methodDataSource)
            {
                AddInstantiation(InferTypesFromMethodDataSource(testClass, methodDataSource, parameters, genericParameterCount), methodDataSource);
            }
        }

        var result = new List<(MethodInfo Method, Type[] TypeArguments, IDataSourceAttribute[]? Rows)>(instantiations.Count);
        foreach (var (closedMethod, typeArguments, rows) in instantiations)
        {
            result.Add((closedMethod, typeArguments, rows?.ToArray()));
        }

        return result;

        void AddInstantiation(Type[]? typeArguments, IDataSourceAttribute? row, bool isMethodArgumentsRow = false)
        {
            if (typeArguments is null)
            {
                return;
            }

            var index = instantiations.FindIndex(x => TypeArrayComparer.Instance.Equals(x.TypeArguments, typeArguments));
            if (index >= 0)
            {
                // [Arguments] rows are processed first, so an instantiation whose rows are [Arguments] rows was
                // created by one
                if (isMethodArgumentsRow
                    && row is not null
                    && instantiations[index].Rows is [ArgumentsAttribute, ..] rows)
                {
                    rows.Add(row);
                }

                return;
            }

            MethodInfo closedMethod;
            try
            {
                closedMethod = method.MakeGenericMethod(typeArguments);
            }
            catch (ArgumentException)
            {
                // Constraint violation: the source generator skips the source as well
                return;
            }

            instantiations.Add((closedMethod, typeArguments, row is null ? null : [row]));
        }
    }

    /// <summary>
    /// Mirrors the generator's <c>InferTypesFromDataSourceAttribute</c> for generic methods: reads the type
    /// arguments of the <c>DataSourceGeneratorAttribute</c> / <c>AsyncDataSourceGeneratorAttribute</c> base.
    /// One type parameter takes the first type argument; otherwise a single tuple argument with one element
    /// per type parameter, or one type argument per type parameter. On a generic class, a source whose type
    /// arguments cover the class type parameters as well infers none: the generator closes the class and the
    /// method over them together, so the class was already closed over them here.
    /// </summary>
    private static Type[]? InferTypesFromTypedDataSource(IDataSourceAttribute dataSource, int genericParameterCount, int classGenericParameterCount)
    {
        for (var baseType = dataSource.GetType().BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (!baseType.IsGenericType)
            {
                continue;
            }

            var definition = baseType.GetGenericTypeDefinition();
            if (definition.Namespace != "TUnit.Core"
                || !(definition.Name.StartsWith("DataSourceGeneratorAttribute`", StringComparison.Ordinal)
                     || definition.Name.StartsWith("AsyncDataSourceGeneratorAttribute`", StringComparison.Ordinal)))
            {
                continue;
            }

            var typeArguments = baseType.GetGenericArguments();

            if (classGenericParameterCount > 0)
            {
                var combinedCount = classGenericParameterCount + genericParameterCount;
                if (typeArguments.Length == combinedCount
                    || (typeArguments is [{ } combinedTuple] && GetValueTupleElementTypes(combinedTuple)?.Length == combinedCount))
                {
                    return null;
                }
            }

            if (genericParameterCount == 1 && typeArguments.Length >= 1)
            {
                return [typeArguments[0]];
            }

            if (typeArguments is [{ } tuple]
                && GetValueTupleElementTypes(tuple) is { } tupleElements
                && tupleElements.Length == genericParameterCount)
            {
                return tupleElements;
            }

            return typeArguments.Length == genericParameterCount ? typeArguments : null;
        }

        return null;
    }

    /// <summary>
    /// Mirrors the generator's <c>InferTypesFromMethodDataSource</c>. It only reads attributes whose first
    /// constructor argument is the member name (<c>[MethodDataSource("Rows")]</c> and
    /// <c>[MethodDataSource&lt;T&gt;("Rows")]</c>), looks for a static parameterless method of that name
    /// declared on the test class (or on <c>T</c>), and requires its declared return type to be
    /// <c>IEnumerable&lt;Func&lt;(...)&gt;&gt;</c>. Each tuple element is matched against the test parameter
    /// at the same position, including type arguments nested in generic parameter types.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflection mode requires dynamic access to data source methods")]
    private static Type[]? InferTypesFromMethodDataSource(
        Type testClass,
        MethodDataSourceAttribute methodDataSource,
        ParameterInfo[] parameters,
        int genericParameterCount)
    {
        var attributeType = methodDataSource.GetType();
        var namedByString = attributeType == typeof(MethodDataSourceAttribute)
            ? methodDataSource.ClassProvidingDataSource is null
            : attributeType.IsGenericType && attributeType.GetGenericTypeDefinition() == typeof(MethodDataSourceAttribute<>);
        if (!namedByString)
        {
            return null;
        }

        // [MethodDataSource<T>("Rows")] reads Rows from T, not from the test class
        var providerType = methodDataSource.ClassProvidingDataSource ?? testClass;
        var dataMethod = providerType.GetMethod(
            methodDataSource.MethodNameProvidingDataSource,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        if (dataMethod?.ReturnType is not { IsGenericType: true } returnType
            || returnType.GetGenericTypeDefinition() != typeof(IEnumerable<>)
            || returnType.GetGenericArguments()[0] is not { IsGenericType: true } funcType
            || funcType.GetGenericTypeDefinition() != typeof(Func<>)
            || GetValueTupleElementTypes(funcType.GetGenericArguments()[0]) is not { } tupleElements)
        {
            return null;
        }

        var typeArguments = new Type?[genericParameterCount];
        for (var i = 0; i < parameters.Length && i < tupleElements.Length; i++)
        {
            MapGenericParameters(parameters[i].ParameterType, tupleElements[i], typeArguments);
        }

        foreach (var typeArgument in typeArguments)
        {
            if (typeArgument is null)
            {
                return null;
            }
        }

        return typeArguments!;
    }

    /// <summary>Mirrors the generator's <c>ProcessTypeForGenerics</c>.</summary>
    private static void MapGenericParameters(Type parameterType, Type actualType, Type?[] typeArguments)
    {
        if (parameterType.IsGenericParameter)
        {
            if (parameterType.DeclaringMethod is not null)
            {
                typeArguments[parameterType.GenericParameterPosition] = actualType;
            }

            return;
        }

        if (parameterType.IsGenericType
            && actualType.IsGenericType
            && parameterType.GetGenericTypeDefinition() == actualType.GetGenericTypeDefinition())
        {
            var parameterArguments = parameterType.GetGenericArguments();
            var actualArguments = actualType.GetGenericArguments();
            for (var i = 0; i < parameterArguments.Length && i < actualArguments.Length; i++)
            {
                MapGenericParameters(parameterArguments[i], actualArguments[i], typeArguments);
            }
        }
    }

    /// <summary>
    /// Returns the logical element types of a <see cref="ValueTuple"/> type, or null for any other type.
    /// The runtime nests elements past the seventh in a <c>TRest</c> tuple, so <c>(int, ..., int)</c> with
    /// eight elements is <c>ValueTuple&lt;int, ..., int, ValueTuple&lt;int&gt;&gt;</c>. This flattens that
    /// nesting, as Roslyn's <c>TupleElements</c> does for the generator.
    /// </summary>
    private static Type[]? GetValueTupleElementTypes(Type type)
    {
        if (!type.IsGenericType || type.Namespace != "System" || !type.Name.StartsWith("ValueTuple`", StringComparison.Ordinal))
        {
            return null;
        }

        var arguments = type.GetGenericArguments();
        if (arguments.Length < 8)
        {
            return arguments;
        }

        if (GetValueTupleElementTypes(arguments[7]) is not { } rest)
        {
            return null;
        }

        var elements = new Type[7 + rest.Length];
        Array.Copy(arguments, elements, 7);
        Array.Copy(rest, 0, elements, 7, rest.Length);
        return elements;
    }

    /// <summary>
    /// Mirrors the generator's <c>InferTypesFromTypeInferringAttributes</c>: a parameter typed by a method type
    /// parameter that carries an attribute implementing <c>IInfersType&lt;T&gt;</c> infers that type parameter.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection mode requires dynamic access; attribute interfaces are only read, not invoked")]
    private static Type[]? InferTypesFromTypeInferringAttributes(ParameterInfo[] parameters, int genericParameterCount)
    {
        Type[]? typeArguments = null;

        foreach (var parameter in parameters)
        {
            var parameterType = parameter.ParameterType;
            if (!parameterType.IsGenericParameter || parameterType.DeclaringMethod is null)
            {
                continue;
            }

            foreach (var attribute in parameter.GetCustomAttributes(inherit: false))
            {
                Type? inferred = null;
                foreach (var implemented in attribute.GetType().GetInterfaces())
                {
                    if (implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(TUnit.Core.Interfaces.IInfersType<>))
                    {
                        inferred = implemented.GetGenericArguments()[0];
                        break;
                    }
                }

                if (inferred is not null)
                {
                    typeArguments ??= new Type[genericParameterCount];
                    typeArguments[parameterType.GenericParameterPosition] = inferred;
                    break;
                }
            }
        }

        if (typeArguments is null)
        {
            return null;
        }

        foreach (var typeArgument in typeArguments)
        {
            if (typeArgument is null)
            {
                return null;
            }
        }

        return typeArguments;
    }

    private static Type[]? InferMethodTypeArguments(object?[] values, ParameterInfo[] parameters, int genericParameterCount)
    {
        var typeArguments = new Type[genericParameterCount];
        var count = Math.Min(values.Length, parameters.Length);

        for (var i = 0; i < count; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (!parameterType.IsGenericParameter || parameterType.DeclaringMethod is null)
            {
                continue;
            }

            // A null value, typed or not, is an element of the params object?[] argument, which Roslyn types
            // as object, so the generator infers object for it ([Arguments((string?)null)] runs as Test<object>(null)).
            typeArguments[parameterType.GenericParameterPosition] = values[i] is { } value
                ? GetArgumentInferenceType(value)
                : typeof(object);
        }

        foreach (var typeArgument in typeArguments)
        {
            if (typeArgument is null)
            {
                return null;
            }
        }

        return typeArguments;
    }

    /// <summary>
    /// The type an [Arguments] value contributes to generic type inference. A <see cref="Type"/> value is a
    /// private RuntimeType instance, but the attribute argument is typed <see cref="Type"/>, which is what the
    /// source generator infers.
    /// </summary>
    private static Type GetArgumentInferenceType(object value) => value is Type ? typeof(Type) : value.GetType();

    private sealed class TypeArrayComparer : IEqualityComparer<Type[]>
    {
        public static readonly TypeArrayComparer Instance = new();

        public bool Equals(Type[]? x, Type[]? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(Type[] obj)
        {
            var hash = 17;
            foreach (var type in obj)
            {
                hash = unchecked(hash * 31 + type.GetHashCode());
            }

            return hash;
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
                Name = TypeNameFormatter.GetNameWithoutGenericArity(type),
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
        var parameters = ctor.GetParameters();

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
    private static void InferGenericTypeMapping(Type paramType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type argType, Dictionary<Type, Type> typeMapping, bool isTopLevel = false)
    {
        // Direct generic parameter
        if (paramType.IsGenericParameter)
        {
            if (isTopLevel)
            {
                // A repeated type parameter takes the type of its last non-null declared argument,
                // which is what TestGenericTypeResolver and source-generated mode infer. Keeping
                // the first (or most specific) type here made the invoker use a different type
                // argument from the one the test was named with.
                typeMapping[paramType] = argType;
            }
            else if (!typeMapping.TryGetValue(paramType, out var existingType) || existingType.IsAssignableFrom(argType))
            {
                // Nested inference (e.g. the element type of a params T[]) must not replace a more
                // specific type already inferred from a leading T argument.
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
    // Mirrors the source generator's GetTrailingElementType: the element type for a trailing
    // collecting parameter — a plain array, or a C# 13 params collection whose target is an
    // array-assignable generic interface (a generated T[] satisfies the parameter).
    private static Type? GetTrailingElementType(ParameterInfo parameter)
    {
        var type = parameter.ParameterType;

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(IEnumerable<>)
                || definition == typeof(ICollection<>)
                || definition == typeof(IList<>)
                || definition == typeof(IReadOnlyCollection<>)
                || definition == typeof(IReadOnlyList<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    // C# 13 params collections are marked with ParamCollectionAttribute rather than
    // ParamArrayAttribute. Match by name so it works across target frameworks that predate the type.
    private static bool IsParamCollection(ParameterInfo parameter)
        => parameter.GetCustomAttributesData()
            .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.ParamCollectionAttribute");

    // Source-generated invokers omit trailing arguments a row leaves out, so the compiler applies
    // the declared defaults. Mirror that for MethodBase.Invoke: Type.Missing makes the runtime use
    // a parameter's default value, where a null slot would bind default(T) instead.
    private static void FillOmittedOptionalArguments(ParameterInfo[] parameters, object?[] castedArgs, int suppliedCount, int endExclusive)
    {
        for (var i = suppliedCount; i < endExclusive; i++)
        {
            if (parameters[i].HasDefaultValue)
            {
                castedArgs[i] = Type.Missing;
            }
        }
    }

    private static bool IsCovariantCompatible(Type paramType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type argType)
    {
        // Only check for generic interface covariance
        if (!paramType.IsInterface || !paramType.IsGenericType)
        {
            return false;
        }

        var paramGenericDef = paramType.GetGenericTypeDefinition();

        if (Array.IndexOf(CovariantInterfaces, paramGenericDef) < 0)
        {
            return false;
        }

        // Check the argument type's interfaces, plus the argument type itself when it's an interface,
        // without allocating a combined array (the old Concat([argType]).ToArray() per call).
        var argInterfaces = AssemblyReferenceCache.GetInterfaces(argType);
        foreach (var iface in argInterfaces)
        {
            if (MatchesCovariantInterface(paramType, paramGenericDef, iface, out var compatible))
            {
                return compatible;
            }
        }

        if (argType.IsInterface && MatchesCovariantInterface(paramType, paramGenericDef, argType, out var argCompatible))
        {
            return argCompatible;
        }

        return false;
    }

    /// <summary>
    /// Known covariant generic interface definitions. Hoisted to avoid allocating the array per
    /// <see cref="IsCovariantCompatible"/> call.
    /// </summary>
    private static readonly Type[] CovariantInterfaces =
    [
        typeof(IEnumerable<>),
        typeof(IReadOnlyList<>),
        typeof(IReadOnlyCollection<>),
        typeof(IEnumerator<>)
    ];

    private static bool MatchesCovariantInterface(Type paramType, Type paramGenericDef, Type iface, out bool compatible)
    {
        if (iface.IsGenericType && iface.GetGenericTypeDefinition() == paramGenericDef)
        {
            var paramElementType = paramType.GetGenericArguments()[0];
            var argElementType = iface.GetGenericArguments()[0];

            // For covariance to work, the parameter element type must be assignable from the argument element type
            // This allows IEnumerable<int> to be passed where IEnumerable<object> is expected
            compatible = paramElementType.IsAssignableFrom(argElementType);
            return true;
        }

        compatible = false;
        return false;
    }

    /// <summary>
    /// Creates a reflection-based test invoker with proper AOT attribution
    /// </summary>
    private static Func<object, object?[], Task> CreateReflectionTestInvoker(Type testClass, MethodInfo testMethod)
    {
        var isPrepared = false;
        // For non-generic methods, parameter shape is stable across invocations — hoist the
        // GetParameters() call out of the hot lambda. Generic method definitions are resolved
        // to a concrete MethodInfo per invocation, so they must still call GetParameters() inline.
        var staticParameters = testMethod.IsGenericMethodDefinition ? null : testMethod.GetParameters();

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
                        if (arg != null && arg != Type.Missing)
                        {
                            var paramType = methodParams[j].ParameterType;
                            var argType = arg.GetType();
                            InferGenericTypeMapping(paramType, argType, typeMapping, isTopLevel: true);
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
                var parameters = staticParameters ?? methodToInvoke.GetParameters();
                var castedArgs = new object?[parameters.Length];

                // Check if the last parameter is a params array, or a plain trailing array
                // parameter (e.g. `string[] names`). Both collect remaining loose arguments
                // into the array, letting `[Arguments(["a", "b"])]` map onto a single array
                // parameter (issue #6120).
                // The engine passes a trailing CancellationToken as the last argument. Exclude it so
                // an array just before it (T[] values, CancellationToken token) collects the row
                // values, as source-gen mode does.
                var dataParameterCount = parameters.Length;
                var dataArgumentCount = args.Length;
                if (parameters.Length > 1
                    && parameters[^1].ParameterType == typeof(CancellationToken)
                    && args.Length > 0
                    && args[^1] is CancellationToken)
                {
                    dataParameterCount--;
                    dataArgumentCount--;
                }

                var lastParam = dataParameterCount > 0 ? parameters[dataParameterCount - 1] : null;
                var paramsElementType = lastParam != null ? GetTrailingElementType(lastParam) : null;
                // A plain trailing array always collects (matches source-gen). A non-array collection
                // only collects when it is a params collection (C# 13, ParamCollectionAttribute).
                var isParamsArray = lastParam != null && paramsElementType != null
                    && (lastParam.ParameterType.IsArray
                        || lastParam.IsDefined(typeof(ParamArrayAttribute), false)
                        || IsParamCollection(lastParam));

                if (isParamsArray && lastParam != null)
                {
                    // Handle params array / params collection parameter
                    var regularParamsCount = dataParameterCount - 1;

                    if (dataParameterCount < parameters.Length)
                    {
                        castedArgs[^1] = args[^1];

                        // Slots padded with Type.Missing for omitted optional parameters are not row values.
                        while (dataArgumentCount > 0 && args[dataArgumentCount - 1] == Type.Missing)
                        {
                            dataArgumentCount--;
                        }
                    }

                    // Process regular parameters first
                    for (var i = 0; i < regularParamsCount && i < dataArgumentCount; i++)
                    {
                        var paramType = parameters[i].ParameterType;
                        var arg = args[i];

                        if (arg == null || arg == Type.Missing)
                        {
                            // Type.Missing marks an omitted optional parameter; Invoke applies its default.
                            castedArgs[i] = arg;
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

                    FillOmittedOptionalArguments(parameters, castedArgs, dataArgumentCount, regularParamsCount);

                    // Collect remaining arguments into params array
                    var paramsStartIndex = regularParamsCount;
                    var paramsCount = Math.Max(0, dataArgumentCount - paramsStartIndex);

                    if (paramsCount == 0
                        && lastParam.HasDefaultValue
                        && !lastParam.IsDefined(typeof(ParamArrayAttribute), false)
                        && !IsParamCollection(lastParam))
                    {
                        // An omitted optional plain array keeps its declared default, as in source-gen mode.
                        castedArgs[regularParamsCount] = Type.Missing;
                    }
                    else if (paramsElementType != null)
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
                                // Mirrors CastHelper.ToTrailingArray used by source-generated invokers.
                                var singleArgType = singleArg.GetType();
                                if (paramsElementType.IsAssignableFrom(singleArgType) || IsCovariantCompatible(paramsElementType, singleArgType))
                                {
                                    // A single element of the array's type - wrap (C# params expansion).
                                    // Checked before the Array branch so `params object[]` still
                                    // receives an int[] as one element, like C#.
                                    var singleElementArray = Array.CreateInstance(paramsElementType, 1);
                                    singleElementArray.SetValue(singleArg, 0);
                                    castedArgs[regularParamsCount] = singleElementArray;
                                }
                                else if (singleArg is Array { Rank: 1 } sourceArray)
                                {
                                    // An array whose runtime type isn't the parameter's (e.g. an object[]
                                    // from a MatrixAttribute subclass for a MyEnum[] parameter, issue #6678):
                                    // convert element-wise rather than forcing it into a single element.
                                    var sourceLowerBound = sourceArray.GetLowerBound(0);
                                    var convertedArray = Array.CreateInstance(paramsElementType, sourceArray.Length);
                                    for (var i = 0; i < sourceArray.Length; i++)
                                    {
                                        convertedArray.SetValue(CastHelper.Cast(paramsElementType, sourceArray.GetValue(sourceLowerBound + i)), i);
                                    }
                                    castedArgs[regularParamsCount] = convertedArray;
                                }
                                else
                                {
                                    // Single scalar needing conversion - convert, then wrap in array
                                    var singleElementArray = Array.CreateInstance(paramsElementType, 1);
                                    singleElementArray.SetValue(CastHelper.Cast(paramsElementType, singleArg), 0);
                                    castedArgs[regularParamsCount] = singleElementArray;
                                }
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

                        if (arg == null || arg == Type.Missing)
                        {
                            // Type.Missing marks an omitted optional parameter; Invoke applies its default.
                            castedArgs[i] = arg;
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

                    FillOmittedOptionalArguments(parameters, castedArgs, args.Length, parameters.Length);
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

        var methodInfo = ExpressionHelper.ExtractMethodInfo(result.TestMethod);

        var testName = GenerateTestName(result.TestClassType, methodInfo);

        var metadata = new DynamicTestMetadata(result)
        {
            TestName = testName,
            TestClassType = result.TestClassType,
            TestMethodName = methodInfo.Name,
            Dependencies = AttributeHelpers.ExtractDependencies(result.Attributes),
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
                var methodInfo = ExpressionHelper.ExtractMethodInfo(result.TestMethod);

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

}
