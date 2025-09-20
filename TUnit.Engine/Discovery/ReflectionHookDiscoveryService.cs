using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Discovers hooks at runtime using reflection for VB.NET and other languages that don't support source generation.
/// </summary>
[RequiresUnreferencedCode("Reflection-based hook discovery requires unreferenced code")]
[RequiresDynamicCode("Hook invocation may require dynamic code generation")]
internal sealed class ReflectionHookDiscoveryService
{
    private static readonly ConcurrentDictionary<Assembly, bool> _scannedAssemblies = new();
    private static int _registrationIndex = 0;

    public static void DiscoverHooks()
    {
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
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        
        foreach (var method in methods)
        {
            // Check for Before attributes
            var beforeAttributes = method.GetCustomAttributes<BeforeAttribute>(false);
            foreach (var attr in beforeAttributes)
            {
                RegisterBeforeHook(type, method, attr, assembly);
            }

            // Check for After attributes
            var afterAttributes = method.GetCustomAttributes<AfterAttribute>(false);
            foreach (var attr in afterAttributes)
            {
                RegisterAfterHook(type, method, attr, assembly);
            }

            // Check for BeforeEvery attributes
            var beforeEveryAttributes = method.GetCustomAttributes<BeforeEveryAttribute>(false);
            foreach (var attr in beforeEveryAttributes)
            {
                RegisterBeforeEveryHook(type, method, attr, assembly);
            }

            // Check for AfterEvery attributes
            var afterEveryAttributes = method.GetCustomAttributes<AfterEveryAttribute>(false);
            foreach (var attr in afterEveryAttributes)
            {
                RegisterAfterEveryHook(type, method, attr, assembly);
            }
        }
    }

    private static void RegisterBeforeHook(Type type, MethodInfo method, BeforeAttribute attr, Assembly assembly)
    {
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
                        HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
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
                        HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestSessionContext>(type, method)
                };
                Sources.AfterTestSessionHooks.Add(sessionHook);
                break;
            case HookType.TestDiscovery:
                var discoveryHook = new AfterTestDiscoveryHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = new DefaultHookExecutor(),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestDiscoveryContext>(type, method)
                };
                Sources.AfterTestDiscoveryHooks.Add(discoveryHook);
                break;
        }
    }

    private static void RegisterBeforeEveryHook(Type type, MethodInfo method, BeforeEveryAttribute attr, Assembly assembly)
    {
        var hookType = attr.HookType;
        var order = attr.Order;
        
        switch (hookType)
        {
            case HookType.Test:
                var testHook = new BeforeTestHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
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
        var hookType = attr.HookType;
        var order = attr.Order;
        
        switch (hookType)
        {
            case HookType.Test:
                var testHook = new AfterTestHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
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
                    HookExecutor = new DefaultHookExecutor(),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestSessionContext>(type, method)
                };
                Sources.AfterTestSessionHooks.Add(sessionHook);
                break;
            case HookType.TestDiscovery:
                var discoveryHook = new AfterTestDiscoveryHookMethod
                {
                    MethodInfo = CreateMethodMetadata(type, method),
                    HookExecutor = new DefaultHookExecutor(),
                    Order = order,
                    RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
                    FilePath = "Unknown",
                    LineNumber = 0,
                    Body = CreateHookDelegate<TestDiscoveryContext>(type, method)
                };
                Sources.AfterTestDiscoveryHooks.Add(discoveryHook);
                break;
        }
    }

    private static void RegisterInstanceBeforeHook(Type type, MethodInfo method, int order)
    {
        var bag = Sources.BeforeTestHooks.GetOrAdd(type, _ => new ConcurrentBag<InstanceHookMethod>());
        var hook = new InstanceHookMethod
        {
            InitClassType = type,
            MethodInfo = CreateMethodMetadata(type, method),
            HookExecutor = new DefaultHookExecutor(),
            Order = order,
            RegistrationIndex = Interlocked.Increment(ref _registrationIndex),
            Body = CreateInstanceHookDelegate(type, method)
        };
        bag.Add(hook);
    }

    private static void RegisterInstanceAfterHook(Type type, MethodInfo method, int order)
    {
        var bag = Sources.AfterTestHooks.GetOrAdd(type, _ => new ConcurrentBag<InstanceHookMethod>());
        var hook = new InstanceHookMethod
        {
            InitClassType = type,
            MethodInfo = CreateMethodMetadata(type, method),
            HookExecutor = new DefaultHookExecutor(),
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
            HookExecutor = new DefaultHookExecutor(),
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
            HookExecutor = new DefaultHookExecutor(),
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
            HookExecutor = new DefaultHookExecutor(),
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
            HookExecutor = new DefaultHookExecutor(),
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
}

// Default hook executor for reflection-discovered hooks
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