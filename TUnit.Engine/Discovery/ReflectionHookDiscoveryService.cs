using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Executors;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building;
using TUnit.Engine.Services;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Discovers hooks at runtime using reflection and populates the Sources dictionaries
/// </summary>
[RequiresUnreferencedCode("Reflection-based hook discovery requires unreferenced code")]
[RequiresDynamicCode("Hook discovery requires dynamic code generation")]
internal static class ReflectionHookDiscoveryService
{
    private static readonly HashSet<Assembly> _scannedAssemblies = [];
    private static readonly Lock _lock = new();
    private static bool _discoveryCompleted = false;

    /// <summary>
    /// Discovers hooks in all relevant assemblies and populates the Sources dictionaries
    /// </summary>
    public static void DiscoverHooks()
    {
        lock (_lock)
        {
            if (_discoveryCompleted)
            {
                return; // Already completed discovery
            }

            _discoveryCompleted = true; // Set completion flag early to prevent re-entry

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(ShouldScanAssembly)
                .ToList();

            foreach (var assembly in assemblies)
            {
                if (!_scannedAssemblies.Add(assembly))
                {
                    continue; // Already scanned
                }

                try
                {
                    DiscoverHooksInAssembly(assembly);
                }
                catch (Exception)
                {
                    // Ignore failures in hook discovery for individual assemblies
                }
            }
        }
    }


    [UnconditionalSuppressMessage("Trimming",
        "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'",
        Justification = "Reflection mode requires dynamic access")]
    private static void DiscoverHooksInAssembly(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException rtle)
        {
            // Some types might fail to load, use the ones that loaded successfully
            types = rtle.Types?.Where(t => t != null).Cast<Type>().ToArray() ?? [];
        }
        catch
        {
            return; // Skip assembly if we can't get types
        }

        foreach (var type in types)
        {
            if (!type.IsClass || type.IsAbstract || IsCompilerGenerated(type))
            {
                continue;
            }

            DiscoverHooksInType(type);
        }
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'",
        Justification = "Reflection mode requires dynamic access")]
    private static void DiscoverHooksInType(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            DiscoverHooksInMethod(type, method);
        }
    }

    private static void DiscoverHooksInMethod(Type type, MethodInfo method)
    {
        // Skip generic methods as they cannot be invoked via reflection without specific type arguments
        if (method.ContainsGenericParameters)
        {
            return;
        }
        
        var attributes = method.GetCustomAttributes();

        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case BeforeAttribute beforeAttr:
                    RegisterBeforeHook(type, method, beforeAttr);
                    break;
                case AfterAttribute afterAttr:
                    RegisterAfterHook(type, method, afterAttr);
                    break;
                case BeforeEveryAttribute beforeEveryAttr:
                    RegisterBeforeEveryHook(type, method, beforeEveryAttr);
                    break;
                case AfterEveryAttribute afterEveryAttr:
                    RegisterAfterEveryHook(type, method, afterEveryAttr);
                    break;
            }
        }
    }

    private static void RegisterBeforeHook(Type type, MethodInfo method, BeforeAttribute attribute)
    {
        var hookType = attribute.HookType;
        var order = attribute.Order;

        switch (hookType)
        {
            case HookType.Test:
                RegisterInstanceHook(Sources.BeforeTestHooks, type, method, order);
                break;
            case HookType.Class:
                RegisterStaticHook(Sources.BeforeClassHooks, type, method, order, CreateBeforeClassHookMethod);
                break;
            case HookType.Assembly:
                RegisterAssemblyHook(Sources.BeforeAssemblyHooks, type.Assembly, method, order, CreateBeforeAssemblyHookMethod);
                break;
            case HookType.TestSession:
                RegisterGlobalHook(Sources.BeforeTestSessionHooks, method, order, CreateBeforeTestSessionHookMethod);
                break;
            case HookType.TestDiscovery:
                RegisterGlobalHook(Sources.BeforeTestDiscoveryHooks, method, order, CreateBeforeTestDiscoveryHookMethod);
                break;
        }
    }

    private static void RegisterAfterHook(Type type, MethodInfo method, AfterAttribute attribute)
    {
        var hookType = attribute.HookType;
        var order = attribute.Order;

        switch (hookType)
        {
            case HookType.Test:
                RegisterInstanceHook(Sources.AfterTestHooks, type, method, order);
                break;
            case HookType.Class:
                RegisterStaticHook(Sources.AfterClassHooks, type, method, order, CreateAfterClassHookMethod);
                break;
            case HookType.Assembly:
                RegisterAssemblyHook(Sources.AfterAssemblyHooks, type.Assembly, method, order, CreateAfterAssemblyHookMethod);
                break;
            case HookType.TestSession:
                RegisterGlobalHook(Sources.AfterTestSessionHooks, method, order, CreateAfterTestSessionHookMethod);
                break;
            case HookType.TestDiscovery:
                RegisterGlobalHook(Sources.AfterTestDiscoveryHooks, method, order, CreateAfterTestDiscoveryHookMethod);
                break;
        }
    }

    private static void RegisterBeforeEveryHook(Type type, MethodInfo method, BeforeEveryAttribute attribute)
    {
        var hookType = attribute.HookType;
        var order = attribute.Order;

        switch (hookType)
        {
            case HookType.Test:
                RegisterGlobalHook(Sources.BeforeEveryTestHooks, method, order, CreateBeforeTestHookMethod);
                break;
            case HookType.Class:
                RegisterGlobalHook(Sources.BeforeEveryClassHooks, method, order, CreateBeforeClassHookMethod);
                break;
            case HookType.Assembly:
                RegisterGlobalHook(Sources.BeforeEveryAssemblyHooks, method, order, CreateBeforeAssemblyHookMethod);
                break;
        }
    }

    private static void RegisterAfterEveryHook(Type type, MethodInfo method, AfterEveryAttribute attribute)
    {
        var hookType = attribute.HookType;
        var order = attribute.Order;

        switch (hookType)
        {
            case HookType.Test:
                RegisterGlobalHook(Sources.AfterEveryTestHooks, method, order, CreateAfterTestHookMethod);
                break;
            case HookType.Class:
                RegisterGlobalHook(Sources.AfterEveryClassHooks, method, order, CreateAfterClassHookMethod);
                break;
            case HookType.Assembly:
                RegisterGlobalHook(Sources.AfterEveryAssemblyHooks, method, order, CreateAfterAssemblyHookMethod);
                break;
        }
    }

    private static void RegisterInstanceHook(ConcurrentDictionary<Type, ConcurrentBag<InstanceHookMethod>> dictionary, Type type, MethodInfo method, int order)
    {
        var bag = dictionary.GetOrAdd(type, _ => []);
        var hookMethod = CreateInstanceHookMethod(method, order);
        bag.Add(hookMethod);
    }

    private static void RegisterStaticHook<T>(ConcurrentDictionary<Type, ConcurrentBag<T>> dictionary, Type type, MethodInfo method, int order, Func<MethodInfo, int, T> factory)
        where T : class
    {
        var bag = dictionary.GetOrAdd(type, _ => []);
        var hookMethod = factory(method, order);
        bag.Add(hookMethod);
    }

    private static void RegisterAssemblyHook<T>(ConcurrentDictionary<Assembly, ConcurrentBag<T>> dictionary, Assembly assembly, MethodInfo method, int order, Func<MethodInfo, int, T> factory)
        where T : class
    {
        var bag = dictionary.GetOrAdd(assembly, _ => []);
        var hookMethod = factory(method, order);
        bag.Add(hookMethod);
    }

    private static void RegisterGlobalHook<T>(ConcurrentBag<T> bag, MethodInfo method, int order, Func<MethodInfo, int, T> factory)
        where T : class
    {
        var hookMethod = factory(method, order);
        bag.Add(hookMethod);
    }

    private static InstanceHookMethod CreateInstanceHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new InstanceHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextBeforeTestHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            InitClassType = method.DeclaringType!,
            Body = CreateInstanceHookDelegate(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static BeforeClassHookMethod CreateBeforeClassHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new BeforeClassHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextBeforeClassHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<ClassHookContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static AfterClassHookMethod CreateAfterClassHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new AfterClassHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextAfterClassHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<ClassHookContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static BeforeAssemblyHookMethod CreateBeforeAssemblyHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new BeforeAssemblyHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextBeforeAssemblyHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<AssemblyHookContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static AfterAssemblyHookMethod CreateAfterAssemblyHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new AfterAssemblyHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextAfterAssemblyHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<AssemblyHookContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static BeforeTestSessionHookMethod CreateBeforeTestSessionHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new BeforeTestSessionHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextBeforeTestSessionHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<TestSessionContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static IHookExecutor GetHookExecutorFromMethod(MethodInfo method)
    {
        // Check for HookExecutorAttribute on the method
        var hookExecutorAttr = method.GetCustomAttribute<HookExecutorAttribute>();
        if (hookExecutorAttr != null)
        {
            try
            {
                return (IHookExecutor)Activator.CreateInstance(hookExecutorAttr.HookExecutorType)!;
            }
            catch
            {
                // Fall back to default if instantiation fails
                return DefaultExecutor.Instance;
            }
        }
        
        return DefaultExecutor.Instance;
    }

    private static void ProcessHookRegistrationEvents(HookMethod hookMethod, MethodInfo method)
    {
        // Find all attributes that implement IHookRegisteredEventReceiver
        var eventReceivers = method.GetCustomAttributes()
            .OfType<IHookRegisteredEventReceiver>()
            .ToList();

        if (eventReceivers.Count == 0)
        {
            return;
        }

        // Create context for hook registration
        var context = new HookRegisteredContext(hookMethod);

        // Call OnHookRegistered for each event receiver
        foreach (var receiver in eventReceivers)
        {
            try
            {
                var task = receiver.OnHookRegistered(context);
                if (task.IsCompleted)
                {
                    // Synchronous completion
                    if (task.IsFaulted)
                    {
                        // Re-throw the exception to be caught below
                        task.GetAwaiter().GetResult();
                    }
                }
                else
                {
                    // For non-completed ValueTask, we need to handle it carefully to avoid deadlocks
                    // Use ConfigureAwait(false) and run synchronously in a safe way
                    if (task.IsCompletedSuccessfully)
                    {
                        // Already successful, nothing to do
                    }
                    else if (task.IsFaulted)
                    {
                        // Re-throw the exception
                        task.GetAwaiter().GetResult();
                    }
                    else
                    {
                        // For truly async operations, execute synchronously using Task.Run to avoid deadlocks
                        Task.Run(async () => await task.ConfigureAwait(false)).GetAwaiter().GetResult();
                    }
                }
            }
            catch
            {
                // Continue processing other receivers if one fails
            }
        }

        // Apply any executor that was set via the context
        if (context.HookExecutor != null)
        {
            hookMethod.HookExecutor = context.HookExecutor;
        }

        // Apply any timeout that was set via the context  
        if (context.Timeout.HasValue)
        {
            hookMethod.Timeout = context.Timeout.Value;
        }
    }

    private static AfterTestSessionHookMethod CreateAfterTestSessionHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new AfterTestSessionHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextAfterTestSessionHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<TestSessionContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static BeforeTestDiscoveryHookMethod CreateBeforeTestDiscoveryHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new BeforeTestDiscoveryHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextBeforeTestDiscoveryHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<BeforeTestDiscoveryContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static AfterTestDiscoveryHookMethod CreateAfterTestDiscoveryHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new AfterTestDiscoveryHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextAfterTestDiscoveryHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<TestDiscoveryContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static BeforeTestHookMethod CreateBeforeTestHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new BeforeTestHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextBeforeEveryTestHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<TestContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    private static AfterTestHookMethod CreateAfterTestHookMethod(MethodInfo method, int order)
    {
        var hookMethod = new AfterTestHookMethod
        {
            MethodInfo = ReflectionMetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method),
            Order = order,
            RegistrationIndex = HookRegistrationIndices.GetNextAfterEveryTestHookIndex(),
            HookExecutor = GetHookExecutorFromMethod(method),
            FilePath = "Unknown",
            LineNumber = 0,
            Body = CreateStaticHookDelegate<TestContext>(method)
        };

        // Process hook registration events (e.g., HookExecutorAttribute)
        ProcessHookRegistrationEvents(hookMethod, method);

        return hookMethod;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    private static Func<object, TestContext, CancellationToken, ValueTask> CreateInstanceHookDelegate(MethodInfo method)
    {
        return (instance, context, cancellationToken) =>
        {
            try
            {
                var parameters = method.GetParameters();
                var args = new object?[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    if (paramType == typeof(TestContext))
                    {
                        args[i] = context;
                    }
                    else if (paramType == typeof(CancellationToken))
                    {
                        args[i] = cancellationToken;
                    }
                    else
                    {
                        args[i] = null;
                    }
                }

                var result = method.Invoke(instance, args);
                if (result is Task task)
                {
                    return new ValueTask(task);
                }
                if (result is ValueTask valueTask)
                {
                    return valueTask;
                }
                return new ValueTask();
            }
            catch (Exception ex)
            {
                return new ValueTask(Task.FromException(ex));
            }
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    private static Func<T, CancellationToken, ValueTask> CreateStaticHookDelegate<T>(MethodInfo method)
    {
        return (context, cancellationToken) =>
        {
            try
            {
                var parameters = method.GetParameters();
                var args = new object?[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    if (paramType == typeof(T))
                    {
                        args[i] = context;
                    }
                    else if (paramType == typeof(CancellationToken))
                    {
                        args[i] = cancellationToken;
                    }
                    else
                    {
                        args[i] = null;
                    }
                }

                var result = method.Invoke(null, args);
                if (result is Task task)
                {
                    return new ValueTask(task);
                }
                if (result is ValueTask valueTask)
                {
                    return valueTask;
                }
                return new ValueTask();
            }
            catch (Exception ex)
            {
                return new ValueTask(Task.FromException(ex));
            }
        };
    }

    private static bool ShouldScanAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name == null)
        {
            return false;
        }

        // Use the same exclusion logic as ReflectionTestDataCollector
        var excludedNames = new[]
        {
            "mscorlib", "System", "System.Core", "System.Runtime", "System.Private.CoreLib",
            "System.Collections", "System.Linq", "System.Threading", "System.Text.RegularExpressions",
            "System.Diagnostics.Debug", "System.Runtime.Extensions", "System.Collections.Concurrent",
            "System.Text.Json", "System.Memory", "System.Net.Http", "System.IO.FileSystem", "System.Console",
            "netstandard", "Microsoft.CSharp", "Microsoft.Win32.Primitives", "Microsoft.Win32.Registry",
            "Microsoft.VisualBasic.Core", "Microsoft.VisualBasic", "TUnit", "TUnit.Core", "TUnit.Engine",
            "TUnit.Assertions", "testhost", "Microsoft.TestPlatform.CoreUtilities",
            "Microsoft.TestPlatform.CommunicationUtilities", "Microsoft.TestPlatform.CrossPlatEngine",
            "Microsoft.TestPlatform.Common", "Microsoft.TestPlatform.PlatformAbstractions",
            "Microsoft.Testing.Platform", "Newtonsoft.Json", "Castle.Core", "Moq", "xunit.core",
            "xunit.assert", "xunit.execution.desktop", "nunit.framework", "FluentAssertions",
            "AutoFixture", "FakeItEasy", "Shouldly", "NSubstitute", "Rhino.Mocks"
        };

        if (excludedNames.Contains(name))
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

        // Check if assembly references TUnit
        // Note: GetReferencedAssemblies() is not supported in AOT mode
        try
        {
            return assembly.GetReferencedAssemblies().Any(a => a.Name != null && (a.Name.StartsWith("TUnit") || a.Name == "TUnit"));
        }
        catch (PlatformNotSupportedException)
        {
            // In AOT mode, GetReferencedAssemblies() is not supported
            // Fall back to scanning all non-system assemblies
            return true;
        }
    }

    private static bool IsCompilerGenerated(Type type)
    {
        return type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false);
    }
}