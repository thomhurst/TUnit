using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Discovery;

/// Discovers tests at runtime using reflection with assembly scanning and caching
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
    private static readonly ConcurrentDictionary<Assembly, Type[]> _assemblyTypesCache = new();

    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        // Disable assembly loading event handler to prevent recursive issues
        // This was causing problems when assemblies were loaded during scanning

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(ShouldScanAssembly)
            .ToList();

        Console.WriteLine($"Scanning {assemblies.Count} assemblies for tests...");

        // Use indexed collection to maintain order
        var resultsByIndex = new ConcurrentDictionary<int, List<TestMetadata>>();

        // Use true parallel processing with thread pool threads
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(assemblies.Select((assembly, index) => new
            {
                assembly, index
            }), parallelOptions, item =>
            {
                var assembly = item.assembly;
                var index = item.index;

                lock (_lock)
                {
                    if (!_scannedAssemblies.Add(assembly))
                    {
                        resultsByIndex[index] =
                        [
                        ];
                        return;
                    }
                }

                try
                {
                    Console.WriteLine($"Scanning assembly: {assembly.GetName().Name}");
                    // Run async method synchronously since we're already on thread pool
                    var testsInAssembly = DiscoverTestsInAssembly(assembly).GetAwaiter().GetResult();
                    resultsByIndex[index] = testsInAssembly.ToList();
                }
                catch (Exception ex)
                {
                    // Create a failed test metadata for the assembly that couldn't be scanned
                    var failedTest = CreateFailedTestMetadataForAssembly(assembly, ex);
                    resultsByIndex[index] =
                    [
                        failedTest
                    ];
                }
            });
        });

        // Reassemble results in original order
        var newTests = new List<TestMetadata>();
        for (var i = 0; i < assemblies.Count; i++)
        {
            if (resultsByIndex.TryGetValue(i, out var tests))
            {
                newTests.AddRange(tests);
            }
        }

        lock (_lock)
        {
            _discoveredTests.AddRange(newTests);


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
            return false;
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
                return asm.GetExportedTypes();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to get exported types from assembly {asm.FullName}: {ex.Message}");
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
                var genericTests = await DiscoverGenericTests(type);
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
                    testMethods = GetAllTestMethods(type)
                        .Where(m => m.IsDefined(typeof(TestAttribute), inherit: false) && !m.IsAbstract)
                        .ToArray();
                }
                else
                {
                    // Only get declared methods
                    testMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(m => m.IsDefined(typeof(TestAttribute), inherit: false) && !m.IsAbstract)
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
                    discoveredTests.Add(await BuildTestMetadata(type, method));
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
        var testMethods = genericTypeDefinition.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.IsDefined(typeof(TestAttribute), inherit: false) && !m.IsAbstract)
            .ToArray();

        if (testMethods.Length == 0)
        {
            return discoveredTests;
        }

        // For each data source combination, create a concrete generic type
        foreach (var dataSource in classDataSources)
        {
            var dataItems = await GetDataFromSourceAsync(dataSource, null!); // TODO

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
                            var testMetadata = await BuildTestMetadata(concreteType, concreteMethod, dataRow);

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

    private static Task<TestMetadata> BuildTestMetadata(Type testClass, MethodInfo testMethod, object?[]? classData = null)
    {
        // Create a base ReflectionTestMetadata instance
        var testName = GenerateTestName(testClass, testMethod);

        try
        {
            return Task.FromResult<TestMetadata>(new ReflectionTestMetadata(testClass, testMethod)
            {
                TestName = testName,
                TestClassType = testClass,
                TestMethodName = testMethod.Name,
                Categories = ReflectionAttributeExtractor.ExtractCategories(testClass, testMethod),
                IsSkipped = ReflectionAttributeExtractor.IsTestSkipped(testClass, testMethod, out var skipReason),
                SkipReason = skipReason,
                TimeoutMs = ReflectionAttributeExtractor.ExtractTimeout(testClass, testMethod),
                RetryCount = ReflectionAttributeExtractor.ExtractRetryCount(testClass, testMethod),
                CanRunInParallel = ReflectionAttributeExtractor.CanRunInParallel(testClass, testMethod),
                Dependencies = ReflectionAttributeExtractor.ExtractDependencies(testClass, testMethod),
                DataSources = ReflectionAttributeExtractor.ExtractDataSources(testMethod),
                ClassDataSources = classData != null 
                    ? [new StaticDataSourceAttribute(new[] { classData })]
                    : ReflectionAttributeExtractor.ExtractDataSources(testClass),
                PropertyDataSources = ReflectionAttributeExtractor.ExtractPropertyDataSources(testClass),
                InstanceFactory = CreateInstanceFactory(testClass)!,
                TestInvoker = CreateTestInvoker(testClass, testMethod),
                ParameterCount = testMethod.GetParameters().Length,
                ParameterTypes = testMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
                TestMethodParameterTypes = testMethod.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name).ToArray(),
                FilePath = ExtractFilePath(testMethod),
                LineNumber = ExtractLineNumber(testMethod),
                MethodMetadata = MetadataBuilder.CreateMethodMetadata(testClass, testMethod),
                GenericTypeInfo = ReflectionGenericTypeResolver.ExtractGenericTypeInfo(testClass),
                GenericMethodInfo = ReflectionGenericTypeResolver.ExtractGenericMethodInfo(testMethod),
                GenericMethodTypeArguments = testMethod.IsGenericMethodDefinition ? null : testMethod.GetGenericArguments(),
                AttributeFactory = () => ReflectionAttributeExtractor.GetAllAttributes(testClass, testMethod),
                PropertyInjections = PropertyInjector.DiscoverInjectableProperties(testClass)
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

        // Default format - just method name to match source generation
        return testMethod.Name;
    }



    [UnconditionalSuppressMessage("Trimming",
        "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' in call to 'System.Type.GetConstructors()'",
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
        return type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
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
            MethodMetadata = MetadataBuilder.CreateMethodMetadata(type, method),
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

        public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
        {
            get => (context, metadata) => new FailedExecutableTest(_exception)
            {
                TestId = context.TestId,
                DisplayName = context.DisplayName,
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
        return args =>
        {
            try
            {
                return ctor.Invoke(args) ?? throw new InvalidOperationException("Failed to create instance");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of {ctor.DeclaringType?.Name}", ex);
            }
        };
    }

    /// <summary>
    /// Creates a reflection-based test invoker with proper AOT attribution
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    private static Func<object, object?[], Task> CreateReflectionTestInvoker(Type testClass, MethodInfo testMethod)
    {
        return (instance, args) =>
        {
            try
            {
                var result = testMethod.Invoke(instance, args);
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

}
