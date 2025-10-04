using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Discovers hooks at runtime using reflection for VB.NET and other languages that don't support source generation.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Reflection mode cannot support trimming")]
[UnconditionalSuppressMessage("Trimming", "IL2055:Call to 'System.Type.MakeGenericType' can not be statically analyzed", Justification = "Reflection mode requires dynamic access")]
[UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
[UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
[UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' requirements", Justification = "Reflection mode requires dynamic access")]
[UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'", Justification = "Reflection mode requires dynamic access")]
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Reflection mode cannot support AOT")]
internal sealed class ReflectionHookDiscoveryService
{
    private static readonly ConcurrentDictionary<Assembly, bool> _scannedAssemblies = new();
    private static readonly ConcurrentDictionary<string, bool> _registeredMethods = new();
    private static int _registrationIndex = 0;
    private static int _discoveryRunCount = 0;

    private static string GetMethodKey(MethodInfo method)
    {
        // Create a unique key for the method based on its signature
        return $"{method.DeclaringType?.FullName}.{method.Name}({string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName))})";
    }

    private static void ClearSourceGeneratedHooks()
    {
        // Clear all hook collections to avoid duplicates when both
        // source generation and reflection discovery run
        Sources.BeforeTestSessionHooks.Clear();
        Sources.AfterTestSessionHooks.Clear();
        Sources.BeforeTestDiscoveryHooks.Clear();
        Sources.AfterTestDiscoveryHooks.Clear();
        Sources.BeforeEveryTestHooks.Clear();
        Sources.AfterEveryTestHooks.Clear();
        Sources.BeforeEveryClassHooks.Clear();
        Sources.AfterEveryClassHooks.Clear();
        Sources.BeforeEveryAssemblyHooks.Clear();
        Sources.AfterEveryAssemblyHooks.Clear();
        Sources.BeforeTestHooks.Clear();
        Sources.AfterTestHooks.Clear();
        Sources.BeforeClassHooks.Clear();
        Sources.AfterClassHooks.Clear();
        Sources.BeforeAssemblyHooks.Clear();
        Sources.AfterAssemblyHooks.Clear();
    }

    /// <summary>
    /// Discovers and registers instance hooks for a specific closed generic type.
    /// This is needed because closed generic types are created at runtime and don't appear in assembly.GetTypes().
    /// </summary>
    public static void DiscoverInstanceHooksForType(Type closedGenericType)
    {
        if (closedGenericType == null || !closedGenericType.IsGenericType || closedGenericType.ContainsGenericParameters)
        {
            return;
        }

        // Check if we've already discovered hooks for this exact closed type
        var methodKey = $"InstanceHooks:{closedGenericType.FullName}";
        if (!_registeredMethods.TryAdd(methodKey, true))
        {
            return; // Already discovered
        }

        // Build inheritance chain from base to derived to ensure hooks execute in correct order
        var inheritanceChain = new List<Type>();
        var current = closedGenericType;
        while (current != null && current != typeof(object))
        {
            inheritanceChain.Insert(0, current); // Insert at front to get base-to-derived order
            current = current.BaseType;
        }

        // Discover hooks in each type in the inheritance chain, from base to derived
        foreach (var typeInChain in inheritanceChain)
        {
            var methods = typeInChain.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(m =>
                {
                    // Get the minimum order from all hook attributes on this method
                    var beforeAttr = m.GetCustomAttribute<BeforeAttribute>();
                    var afterAttr = m.GetCustomAttribute<AfterAttribute>();
                    var beforeEveryAttr = m.GetCustomAttribute<BeforeEveryAttribute>();
                    var afterEveryAttr = m.GetCustomAttribute<AfterEveryAttribute>();

                    var orders = new List<int>();
                    if (beforeAttr != null) orders.Add(beforeAttr.Order);
                    if (afterAttr != null) orders.Add(afterAttr.Order);
                    if (beforeEveryAttr != null) orders.Add(beforeEveryAttr.Order);
                    if (afterEveryAttr != null) orders.Add(afterEveryAttr.Order);

                    return orders.Any() ? orders.Min() : 0;
                })
                .ThenBy(m => m.MetadataToken) // Then sort by MetadataToken to preserve source file order
                .ToArray();

            foreach (var method in methods)
            {
                // Check for Before attributes
                var beforeAttributes = method.GetCustomAttributes<BeforeAttribute>(false);
                foreach (var attr in beforeAttributes)
                {
                    if (attr.HookType == HookType.Test && !method.IsStatic)
                    {
                        RegisterInstanceBeforeHook(typeInChain, method, attr.Order);
                    }
                }

                // Check for After attributes
                var afterAttributes = method.GetCustomAttributes<AfterAttribute>(false);
                foreach (var attr in afterAttributes)
                {
                    if (attr.HookType == HookType.Test && !method.IsStatic)
                    {
                        RegisterInstanceAfterHook(typeInChain, method, attr.Order);
                    }
                }
            }
        }
    }

    public static void DiscoverHooks()
    {
        // Prevent running hook discovery multiple times in the same process
        // This can happen when both discovery and execution run in the same process
        if (Interlocked.Increment(ref _discoveryRunCount) > 1)
        {
            return;
        }

        // Clear source-generated hooks since we're discovering via reflection
        // In reflection mode, source generation may have already populated Sources
        // We need to clear them to avoid duplicates
        ClearSourceGeneratedHooks();

#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new Exception("Using TUnit Reflection mechanisms isn't supported in AOT mode");
        }
#endif

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (ShouldScanAssembly(assembly))
            {
                DiscoverHooksInAssembly(assembly);
            }
        }
    }

    private static bool ShouldScanAssembly(Assembly assembly)
    {
        if (_scannedAssemblies.ContainsKey(assembly))
        {
            return false;
        }

        var name = assembly.GetName().Name;
        if (name == null)
        {
            return false;
        }

        // Skip system and framework assemblies
        if (name.StartsWith("System.") ||
            name.StartsWith("Microsoft.") ||
            name == "mscorlib" ||
            name == "netstandard" ||
            name == "testhost")
        {
            return false;
        }

        // Skip TUnit framework assemblies (but not test projects)
        if (name == "TUnit" ||
            name == "TUnit.Core" ||
            name == "TUnit.Engine" ||
            name == "TUnit.Assertions")
        {
            return false;
        }

        if (assembly.IsDynamic)
        {
            return false;
        }

        // Check if assembly references TUnit.Core
        var referencedAssemblies = assembly.GetReferencedAssemblies();
        var referencesTUnit = false;
        foreach (var refAssembly in referencedAssemblies)
        {
            if (refAssembly.Name == "TUnit.Core")
            {
                referencesTUnit = true;
                break;
            }
        }

        return referencesTUnit;
    }

    private static void DiscoverHooksInAssembly(Assembly assembly)
    {
        if (!_scannedAssemblies.TryAdd(assembly, true))
        {
            return;
        }

        try
        {
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                DiscoverHooksInType(type, assembly);
            }
        }
        catch
        {
            // Ignore assemblies that can't be scanned
        }
    }

    private static void DiscoverHooksInType(Type type, Assembly assembly)
    {
        // Build inheritance chain from base to derived to ensure hooks execute in correct order
        var inheritanceChain = new List<Type>();
        var current = type;
        while (current != null && current != typeof(object))
        {
            inheritanceChain.Insert(0, current); // Insert at front to get base-to-derived order
            current = current.BaseType;
        }

        // Discover hooks in each type in the inheritance chain, from base to derived
        foreach (var typeInChain in inheritanceChain)
        {
            // Use DeclaredOnly to get methods defined in this specific type, not inherited ones
            var methods = typeInChain.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .OrderBy(m =>
                {
                    // Get the minimum order from all hook attributes on this method
                    var beforeAttr = m.GetCustomAttribute<BeforeAttribute>();
                    var afterAttr = m.GetCustomAttribute<AfterAttribute>();
                    var beforeEveryAttr = m.GetCustomAttribute<BeforeEveryAttribute>();
                    var afterEveryAttr = m.GetCustomAttribute<AfterEveryAttribute>();

                    var orders = new List<int>();
                    if (beforeAttr != null) orders.Add(beforeAttr.Order);
                    if (afterAttr != null) orders.Add(afterAttr.Order);
                    if (beforeEveryAttr != null) orders.Add(beforeEveryAttr.Order);
                    if (afterEveryAttr != null) orders.Add(afterEveryAttr.Order);

                    return orders.Any() ? orders.Min() : 0;
                })
                .ThenBy(m => m.MetadataToken) // Then sort by MetadataToken to preserve source file order
                .ToArray();

            foreach (var method in methods)
            {
                // Check for Before attributes
                var beforeAttributes = method.GetCustomAttributes<BeforeAttribute>(false);
                foreach (var attr in beforeAttributes)
                {
                    RegisterBeforeHook(typeInChain, method, attr, assembly);
                }

                // Check for After attributes
                var afterAttributes = method.GetCustomAttributes<AfterAttribute>(false);
                foreach (var attr in afterAttributes)
                {
                    RegisterAfterHook(typeInChain, method, attr, assembly);
                }

                // Check for BeforeEvery attributes
                var beforeEveryAttributes = method.GetCustomAttributes<BeforeEveryAttribute>(false);
                foreach (var attr in beforeEveryAttributes)
                {
                    RegisterBeforeEveryHook(typeInChain, method, attr, assembly);
                }

                // Check for AfterEvery attributes
                var afterEveryAttributes = method.GetCustomAttributes<AfterEveryAttribute>(false);
                foreach (var attr in afterEveryAttributes)
                {
                    RegisterAfterEveryHook(typeInChain, method, attr, assembly);
                }
            }
        }
    }

    private static void RegisterBeforeHook(Type type, MethodInfo method, BeforeAttribute attr, Assembly assembly)
    {
        // Prevent duplicate registrations of the same method
        var methodKey = GetMethodKey(method);
        if (!_registeredMethods.TryAdd(methodKey, true))
        {
            return;
        }

        var hookType = attr.HookType;
        var order = attr.Order;

        switch (hookType)
        {
            case HookType.Test:
                if (method.IsStatic)
                {
                    var hook = new BeforeTestHookMethod
                    {
                        MethodInfo = CreateMethodMetadata(type, method),
                        HookExecutor = GetHookExecutor(method),
                        Order = order,
                        RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                        FilePath = "Unknown",
                        LineNumber = 0,
                        Body = CreateHookDelegate<TestContext>(type, method)
                    };
                    Sources.BeforeEveryTestHooks.Add(hook);
                }
                else
                {
                    RegisterInstanceBeforeHook(type, method, order);
                }
                break;
            case HookType.Class:
                RegisterBeforeClassHook(type, method, order);
                break;
            case HookType.Assembly:
                RegisterBeforeAssemblyHook(assembly, type, method, order);
                break;
            case HookType.TestSession:
                var sessionHook = new BeforeTestSessionHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestSessionContext>(type, method)
                };
                Sources.BeforeTestSessionHooks.Add(sessionHook);
                break;
            case HookType.TestDiscovery:
                // BeforeEvery(TestDiscovery) is treated the same as Before(TestDiscovery)
                // The source generator ignores the "Every" suffix for TestDiscovery hooks
                // Register it as a regular Before hook to match source-gen behavior
                var discoveryHook = new BeforeTestDiscoveryHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<BeforeTestDiscoveryContext>(type, method)
                };
                Sources.BeforeTestDiscoveryHooks.Add(discoveryHook);
                break;
        }
    }

    private static void RegisterAfterHook(Type type, MethodInfo method, AfterAttribute attr, Assembly assembly)
    {
        // Prevent duplicate registrations of the same method
        var methodKey = GetMethodKey(method);
        if (!_registeredMethods.TryAdd(methodKey, true))
        {
            return;
        }

        var hookType = attr.HookType;
        var order = attr.Order;

        switch (hookType)
        {
            case HookType.Test:
                if (method.IsStatic)
                {
                    var hook = new AfterTestHookMethod
                    {
                        MethodInfo = CreateMethodMetadata(type, method),
                        HookExecutor = GetHookExecutor(method),
                        Order = order,
                        RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                        FilePath = "Unknown",
                        LineNumber = 0,
                        Body = CreateHookDelegate<TestContext>(type, method)
                    };
                    Sources.AfterEveryTestHooks.Add(hook);
                }
                else
                {
                    RegisterInstanceAfterHook(type, method, order);
                }
                break;
            case HookType.Class:
                RegisterAfterClassHook(type, method, order);
                break;
            case HookType.Assembly:
                RegisterAfterAssemblyHook(assembly, type, method, order);
                break;
            case HookType.TestSession:
                var sessionHook = new AfterTestSessionHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestSessionContext>(type, method)
                };
                Sources.AfterTestSessionHooks.Add(sessionHook);
                break;
            case HookType.TestDiscovery:
                var discoveryMetadata = CreateMethodMetadata(type, method);
                // Check if this hook is already registered (prevent duplicates)
                if (!Sources.AfterTestDiscoveryHooks.Any(h => h.MethodInfo.Name == discoveryMetadata.Name &&
                                                               h.MethodInfo.Type == discoveryMetadata.Type))
                {
                    var discoveryHook = new AfterTestDiscoveryHookMethod
                    {
                        MethodInfo = discoveryMetadata,
                        HookExecutor = GetHookExecutor(method),
                        Order = order,
                        RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                        FilePath = "Unknown",
                        LineNumber = 0,
                        Body = CreateHookDelegate<TestDiscoveryContext>(type, method)
                    };
                    Sources.AfterTestDiscoveryHooks.Add(discoveryHook);
                }
                break;
        }
    }

    private static void RegisterBeforeEveryHook(Type type, MethodInfo method, BeforeEveryAttribute attr, Assembly assembly)
    {
        // Prevent duplicate registrations of the same method
        var methodKey = GetMethodKey(method);
        if (!_registeredMethods.TryAdd(methodKey, true))
        {
            return;
        }

        var hookType = attr.HookType;
        var order = attr.Order;

        switch (hookType)
        {
            case HookType.Test:
                var testHook = new BeforeTestHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestContext>(type, method)
                };
                Sources.BeforeEveryTestHooks.Add(testHook);
                break;
            case HookType.Class:
                var classHook = new BeforeClassHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<ClassHookContext>(type, method)
                };
                Sources.BeforeEveryClassHooks.Add(classHook);
                break;
            case HookType.Assembly:
                var assemblyHook = new BeforeAssemblyHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<AssemblyHookContext>(type, method)
                };
                Sources.BeforeEveryAssemblyHooks.Add(assemblyHook);
                break;
            case HookType.TestSession:
                var sessionHook = new BeforeTestSessionHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestSessionContext>(type, method)
                };
                Sources.BeforeTestSessionHooks.Add(sessionHook);
                break;
            case HookType.TestDiscovery:
                var discoveryHook = new BeforeTestDiscoveryHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<BeforeTestDiscoveryContext>(type, method)
                };
                Sources.BeforeTestDiscoveryHooks.Add(discoveryHook);
                break;
        }
    }

    private static void RegisterAfterEveryHook(Type type, MethodInfo method, AfterEveryAttribute attr, Assembly assembly)
    {
        // Prevent duplicate registrations of the same method
        var methodKey = GetMethodKey(method);
        if (!_registeredMethods.TryAdd(methodKey, true))
        {
            return;
        }

        var hookType = attr.HookType;
        var order = attr.Order;

        switch (hookType)
        {
            case HookType.Test:
                var testHook = new AfterTestHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestContext>(type, method)
                };
                Sources.AfterEveryTestHooks.Add(testHook);
                break;
            case HookType.Class:
                var classHook = new AfterClassHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<ClassHookContext>(type, method)
                };
                Sources.AfterEveryClassHooks.Add(classHook);
                break;
            case HookType.Assembly:
                var assemblyHook = new AfterAssemblyHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<AssemblyHookContext>(type, method)
                };
                Sources.AfterEveryAssemblyHooks.Add(assemblyHook);
                break;
            case HookType.TestSession:
                var sessionHook = new AfterTestSessionHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = GetHookExecutor(method),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestSessionContext>(type, method)
                };
                Sources.AfterTestSessionHooks.Add(sessionHook);
                break;
            case HookType.TestDiscovery:
                // AfterEvery(TestDiscovery) is treated the same as After(TestDiscovery)
                // The source generator ignores the "Every" suffix for TestDiscovery hooks
                // Register it as a regular After hook to match source-gen behavior
                var discoveryEveryMetadata = CreateMethodMetadata(type, method);
                // Check if this hook is already registered (prevent duplicates)
                if (!Sources.AfterTestDiscoveryHooks.Any(h => h.MethodInfo.Name == discoveryEveryMetadata.Name &&
                                                               h.MethodInfo.Type == discoveryEveryMetadata.Type))
                {
                    var discoveryHook = new AfterTestDiscoveryHookMethod
                    {
                        MethodInfo = discoveryEveryMetadata,
                        HookExecutor = GetHookExecutor(method),
                        Order = order,
                        RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                        FilePath = "Unknown",
                        LineNumber = 0,
                        Body = CreateHookDelegate<TestDiscoveryContext>(type, method)
                    };
                    Sources.AfterTestDiscoveryHooks.Add(discoveryHook);
                }
                break;
        }
    }

    private static void RegisterInstanceBeforeHook(Type type, MethodInfo method, int order)
    {
        // Instance hooks on open generic types will be registered when closed types are discovered
        if (type.ContainsGenericParameters)
        {
            return;
        }

        var bag = Sources.BeforeTestHooks.GetOrAdd(type, _ => new ConcurrentBag<InstanceHookMethod>());
        var hook = new InstanceHookMethod
        {
            InitClassType = type,
            MethodInfo = CreateMethodMetadata(type, method),
            HookExecutor = GetHookExecutor(method),
            Order = order,
            RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
            Body = CreateInstanceHookDelegate(type, method)
        };
        bag.Add(hook);
    }

    private static void RegisterInstanceAfterHook(Type type, MethodInfo method, int order)
    {
        // Instance hooks on open generic types will be registered when closed types are discovered
        if (type.ContainsGenericParameters)
        {
            return;
        }

        var bag = Sources.AfterTestHooks.GetOrAdd(type, _ => new ConcurrentBag<InstanceHookMethod>());
        var hook = new InstanceHookMethod
        {
            InitClassType = type,
            MethodInfo = CreateMethodMetadata(type, method),
            HookExecutor = GetHookExecutor(method),
            Order = order,
            RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
            Body = CreateInstanceHookDelegate(type, method)
        };
        bag.Add(hook);
    }

    private static void RegisterBeforeClassHook(Type type, MethodInfo method, int order)
    {
        var bag = Sources.BeforeClassHooks.GetOrAdd(type, _ => new ConcurrentBag<BeforeClassHookMethod>());
        var hook = new BeforeClassHookMethod
        {
            MethodInfo = CreateMethodMetadata(type, method),
            HookExecutor = GetHookExecutor(method),
            Order = order,
            RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateHookDelegate<ClassHookContext>(type, method)
        };
        bag.Add(hook);
    }

    private static void RegisterAfterClassHook(Type type, MethodInfo method, int order)
    {
        var bag = Sources.AfterClassHooks.GetOrAdd(type, _ => new ConcurrentBag<AfterClassHookMethod>());
        var hook = new AfterClassHookMethod
        {
            MethodInfo = CreateMethodMetadata(type, method),
            HookExecutor = GetHookExecutor(method),
            Order = order,
            RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateHookDelegate<ClassHookContext>(type, method)
        };
        bag.Add(hook);
    }

    private static void RegisterBeforeAssemblyHook(Assembly assembly, Type type, MethodInfo method, int order)
    {
        var bag = Sources.BeforeAssemblyHooks.GetOrAdd(assembly, _ => new ConcurrentBag<BeforeAssemblyHookMethod>());
        var hook = new BeforeAssemblyHookMethod
        {
            MethodInfo = CreateMethodMetadata(type, method),
            HookExecutor = GetHookExecutor(method),
            Order = order,
            RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateHookDelegate<AssemblyHookContext>(type, method)
        };
        bag.Add(hook);
    }

    private static void RegisterAfterAssemblyHook(Assembly assembly, Type type, MethodInfo method, int order)
    {
        var bag = Sources.AfterAssemblyHooks.GetOrAdd(assembly, _ => new ConcurrentBag<AfterAssemblyHookMethod>());
        var hook = new AfterAssemblyHookMethod
        {
            MethodInfo = CreateMethodMetadata(type, method),
            HookExecutor = GetHookExecutor(method),
            Order = order,
            RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateHookDelegate<AssemblyHookContext>(type, method)
        };
        bag.Add(hook);
    }

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
                Assembly = new AssemblyMetadata
                {
                    Name = type.Assembly.GetName().Name ?? "Unknown"
                },
                Parameters = [],
                Properties = [],
                Parent = null
            },
            Parameters = method.GetParameters().Select(p => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? string.Empty,
                Type = p.ParameterType,
                TypeReference = TypeReference.CreateConcrete(p.ParameterType.AssemblyQualifiedName!),
                ReflectionInfo = p
            }).ToArray(),
            GenericTypeCount = 0,
            ReturnTypeReference = TypeReference.CreateConcrete(method.ReturnType.AssemblyQualifiedName!),
            ReturnType = method.ReturnType,
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName!)
        };
    }

    private static Func<object, TestContext, CancellationToken, ValueTask> CreateInstanceHookDelegate(Type type, MethodInfo method)
    {
        return async (instance, context, cancellationToken) =>
        {
            var parameters = method.GetParameters();
            object?[] args;

            if (parameters.Length == 0)
            {
                args = [];
            }
            else if (parameters.Length == 1)
            {
                if (parameters[0].ParameterType == typeof(CancellationToken))
                {
                    args = [cancellationToken];
                }
                else
                {
                    args = [context];
                }
            }
            else if (parameters.Length == 2)
            {
                args = [context, cancellationToken];
            }
            else
            {
                throw new InvalidOperationException($"Hook method {method.Name} has invalid parameters");
            }

            var result = method.Invoke(instance, args);

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

    private static Func<T, CancellationToken, ValueTask> CreateHookDelegate<T>(Type type, MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = method.GetParameters();
            object?[] args;
            object? instance = method.IsStatic ? null : Activator.CreateInstance(type);

            if (parameters.Length == 0)
            {
                args = [];
            }
            else if (parameters.Length == 1)
            {
                if (parameters[0].ParameterType == typeof(CancellationToken))
                {
                    args = [cancellationToken];
                }
                else
                {
                    args = [context];
                }
            }
            else if (parameters.Length == 2)
            {
                args = [context, cancellationToken];
            }
            else
            {
                throw new InvalidOperationException($"Hook method {method.Name} has invalid parameters");
            }

            var result = method.Invoke(instance, args);

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

    /// <summary>
    /// Extracts the HookExecutor from method attributes, or returns DefaultHookExecutor if not found
    /// </summary>
    private static IHookExecutor GetHookExecutor(MethodInfo method)
    {
        // Look for HookExecutorAttribute on the method
        var hookExecutorAttr = method.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType().Name == "HookExecutorAttribute" ||
                                a.GetType().BaseType?.Name == "HookExecutorAttribute");

        if (hookExecutorAttr != null)
        {
            // Get the HookExecutorType property
            var hookExecutorTypeProperty = hookExecutorAttr.GetType().GetProperty("HookExecutorType");
            if (hookExecutorTypeProperty != null)
            {
                var executorType = hookExecutorTypeProperty.GetValue(hookExecutorAttr) as Type;
                if (executorType != null)
                {
                    try
                    {
                        // Instantiate the executor
                        var executor = Activator.CreateInstance(executorType) as IHookExecutor;
                        if (executor != null)
                        {
                            return executor;
                        }
                    }
                    catch
                    {
                        // Fall back to default if instantiation fails
                    }
                }
            }
        }

        return new DefaultHookExecutor();
    }
}

internal class DefaultHookExecutor : IHookExecutor
{
    public ValueTask ExecuteBeforeTestDiscoveryHook(MethodMetadata testMethod, BeforeTestDiscoveryContext context, Func<ValueTask> action)
        => action();

    public ValueTask ExecuteAfterTestDiscoveryHook(MethodMetadata testMethod, TestDiscoveryContext context, Func<ValueTask> action)
        => action();

    public ValueTask ExecuteBeforeAssemblyHook(MethodMetadata testMethod, AssemblyHookContext context, Func<ValueTask> action)
        => action();

    public ValueTask ExecuteAfterAssemblyHook(MethodMetadata testMethod, AssemblyHookContext context, Func<ValueTask> action)
        => action();

    public ValueTask ExecuteBeforeClassHook(MethodMetadata testMethod, ClassHookContext context, Func<ValueTask> action)
        => action();

    public ValueTask ExecuteAfterClassHook(MethodMetadata testMethod, ClassHookContext context, Func<ValueTask> action)
        => action();

    public ValueTask ExecuteBeforeTestHook(MethodMetadata testMethod, TestContext context, Func<ValueTask> action)
        => action();

    public ValueTask ExecuteAfterTestHook(MethodMetadata testMethod, TestContext context, Func<ValueTask> action)
        => action();

    public ValueTask ExecuteBeforeTestSessionHook(MethodMetadata testMethod, TestSessionContext context, Func<ValueTask> action)
        => action();

    public ValueTask ExecuteAfterTestSessionHook(MethodMetadata testMethod, TestSessionContext context, Func<ValueTask> action)
        => action();
}
