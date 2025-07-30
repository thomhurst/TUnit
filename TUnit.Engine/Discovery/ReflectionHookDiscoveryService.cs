using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building;
using TUnit.Engine.Services;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Discovers hooks from assemblies using reflection and populates the Sources dictionaries
/// </summary>
[RequiresUnreferencedCode("Reflection-based hook discovery requires unreferenced code")]
internal sealed class ReflectionHookDiscoveryService
{
    private static readonly HashSet<Assembly> _scannedAssemblies = new();
    private static readonly object _lock = new();
    private readonly IHookExecutor _hookExecutor;
    
    public ReflectionHookDiscoveryService(IHookExecutor hookExecutor)
    {
        _hookExecutor = hookExecutor;
    }

    public Task DiscoverHooksInAssembliesAsync(IEnumerable<Assembly> assemblies)
    {
        var assembliesToScan = assemblies.Where(assembly =>
        {
            lock (_lock)
            {
                return _scannedAssemblies.Add(assembly);
            }
        }).ToList();

        foreach (var assembly in assembliesToScan)
        {
            DiscoverHooksInAssembly(assembly);
        }
        
        return Task.CompletedTask;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Using member 'System.Reflection.Assembly.GetExportedTypes()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'")]
    private void DiscoverHooksInAssembly(Assembly assembly)
    {
        try
        {
            var types = assembly.GetExportedTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !IsCompilerGenerated(t));

            foreach (var type in types)
            {
                DiscoverHooksInType(type, assembly);
            }

            // Discover assembly-level hooks (static methods in any type)
            DiscoverAssemblyLevelHooks(assembly, types);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to discover hooks in assembly {assembly.FullName}: {ex.Message}");
        }
    }

    private void DiscoverHooksInType(Type type, Assembly assembly)
    {
        try
        {
            var methods = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                // Process Before attributes
                var beforeAttrs = method.GetCustomAttributes<BeforeAttribute>(inherit: false);
                foreach (var attr in beforeAttrs)
                {
                    if (attr.HookType.HasFlag(HookType.TestDiscovery))
                    {
                        AddTestDiscoveryHook(method, attr, isBeforeHook: true);
                    }
                    else
                    {
                        AddRuntimeHook(type, method, attr, isBeforeHook: true);
                    }
                }

                // Process After attributes
                var afterAttrs = method.GetCustomAttributes<AfterAttribute>(inherit: false);
                foreach (var attr in afterAttrs)
                {
                    if (attr.HookType.HasFlag(HookType.TestDiscovery))
                    {
                        AddTestDiscoveryHook(method, attr, isBeforeHook: false);
                    }
                    else
                    {
                        AddRuntimeHook(type, method, attr, isBeforeHook: false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to discover hooks in type {type.FullName}: {ex.Message}");
        }
    }

    private void DiscoverAssemblyLevelHooks(Assembly assembly, IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            try
            {
                var methods = type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static | BindingFlags.DeclaredOnly);

                foreach (var method in methods)
                {
                    // Check for assembly-level Before hooks
                    var beforeAttrs = method.GetCustomAttributes<BeforeAttribute>()
                        .Where(a => a.HookType.HasFlag(HookType.Assembly));
                    
                    foreach (var attr in beforeAttrs)
                    {
                        if (!attr.HookType.HasFlag(HookType.TestDiscovery))
                        {
                            AddAssemblyHook(assembly, method, attr, isBeforeHook: true);
                        }
                    }

                    // Check for assembly-level After hooks
                    var afterAttrs = method.GetCustomAttributes<AfterAttribute>()
                        .Where(a => a.HookType.HasFlag(HookType.Assembly));
                    
                    foreach (var attr in afterAttrs)
                    {
                        if (!attr.HookType.HasFlag(HookType.TestDiscovery))
                        {
                            AddAssemblyHook(assembly, method, attr, isBeforeHook: false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to discover assembly hooks in type {type.FullName}: {ex.Message}");
            }
        }
    }

    private void AddRuntimeHook(Type type, MethodInfo method, HookAttribute attr, bool isBeforeHook)
    {
        var methodMetadata = MetadataBuilder.CreateMethodMetadata(type, method);

        if (attr.HookType.HasFlag(HookType.Class))
        {
            var hook = new BeforeClassHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = _hookExecutor,
                Order = attr.Order,
                FilePath = "Unknown", // Assembly.Location not available in single-file apps
                LineNumber = 0,
                Body = CreateClassHookBody(method)
            };

            if (isBeforeHook)
            {
                Sources.BeforeClassHooks.GetOrAdd(type, _ => new ConcurrentBag<StaticHookMethod<ClassHookContext>>()).Add(hook);
            }
            else
            {
                Sources.AfterClassHooks.GetOrAdd(type, _ => new ConcurrentBag<StaticHookMethod<ClassHookContext>>()).Add(hook);
            }
        }

        if (attr.HookType.HasFlag(HookType.Test))
        {
            if (method.IsStatic)
            {
                var hook = new BeforeTestHookMethod
                {
                    MethodInfo = methodMetadata,
                    HookExecutor = _hookExecutor,
                    Order = attr.Order,
                    FilePath = "Unknown", // Assembly.Location not available in single-file apps
                    LineNumber = 0,
                    Body = CreateStaticTestHookBody(method)
                };

                if (isBeforeHook)
                {
                    Sources.BeforeEveryTestHooks.Add(hook);
                }
                else
                {
                    Sources.AfterEveryTestHooks.Add(hook);
                }
            }
            else
            {
                var hook = new InstanceHookMethod
                {
                    ClassType = type,
                    MethodInfo = methodMetadata,
                    HookExecutor = _hookExecutor,
                    Order = attr.Order,
                    Body = CreateInstanceHookBody(method)
                };

                if (isBeforeHook)
                {
                    Sources.BeforeTestHooks.GetOrAdd(type, _ => new ConcurrentBag<InstanceHookMethod>()).Add(hook);
                }
                else
                {
                    Sources.AfterTestHooks.GetOrAdd(type, _ => new ConcurrentBag<InstanceHookMethod>()).Add(hook);
                }
            }
        }

        if (attr.HookType.HasFlag(HookType.TestSession))
        {
            var hook = new BeforeTestSessionHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = _hookExecutor,
                Order = attr.Order,
                FilePath = "Unknown", // Assembly.Location not available in single-file apps
                LineNumber = 0,
                Body = CreateTestSessionHookBody(method)
            };

            if (isBeforeHook)
            {
                Sources.BeforeTestSessionHooks.Add(hook);
            }
            else
            {
                Sources.AfterTestSessionHooks.Add(hook);
            }
        }
    }

    private void AddAssemblyHook(Assembly assembly, MethodInfo method, HookAttribute attr, bool isBeforeHook)
    {
        var methodMetadata = MetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method);
        
        var hook = new BeforeAssemblyHookMethod
        {
            MethodInfo = methodMetadata,
            HookExecutor = _hookExecutor,
            Order = attr.Order,
            FilePath = "Unknown", // Assembly.Location not available in single-file apps
            LineNumber = 0,
            Body = CreateAssemblyHookBody(method)
        };

        if (isBeforeHook)
        {
            Sources.BeforeAssemblyHooks.GetOrAdd(assembly, _ => new ConcurrentBag<StaticHookMethod<AssemblyHookContext>>()).Add(hook);
        }
        else
        {
            Sources.AfterAssemblyHooks.GetOrAdd(assembly, _ => new ConcurrentBag<StaticHookMethod<AssemblyHookContext>>()).Add(hook);
        }
    }

    private void AddTestDiscoveryHook(MethodInfo method, HookAttribute attr, bool isBeforeHook)
    {
        var methodMetadata = MetadataBuilder.CreateMethodMetadata(method.DeclaringType!, method);
        
        if (isBeforeHook)
        {
            var hook = new BeforeTestDiscoveryHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = _hookExecutor,
                Order = attr.Order,
                FilePath = "Unknown", // Assembly.Location not available in single-file apps
                LineNumber = 0,
                Body = CreateBeforeTestDiscoveryHookBody(method)
            };
            Sources.BeforeTestDiscoveryHooks.Add(hook);
        }
        else
        {
            var hook = new AfterTestDiscoveryHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = _hookExecutor,
                Order = attr.Order,
                FilePath = "Unknown", // Assembly.Location not available in single-file apps
                LineNumber = 0,
                Body = CreateAfterTestDiscoveryHookBody(method)
            };
            Sources.AfterTestDiscoveryHooks.Add(hook);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method parameter does not satisfy annotation requirements")]
    private Func<object, TestContext, CancellationToken, ValueTask>? CreateInstanceHookBody(MethodInfo method)
    {
        return async (instance, context, cancellationToken) =>
        {
            var parameters = BuildHookParameters(method, context, cancellationToken);
            var result = method.Invoke(instance, parameters);
            await HandleHookResult(result);
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method parameter does not satisfy annotation requirements")]
    private Func<TestContext, CancellationToken, ValueTask>? CreateStaticTestHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = BuildHookParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await HandleHookResult(result);
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method parameter does not satisfy annotation requirements")]
    private Func<ClassHookContext, CancellationToken, ValueTask>? CreateClassHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = BuildHookParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await HandleHookResult(result);
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method parameter does not satisfy annotation requirements")]
    private Func<AssemblyHookContext, CancellationToken, ValueTask>? CreateAssemblyHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = BuildHookParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await HandleHookResult(result);
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method parameter does not satisfy annotation requirements")]
    private Func<TestSessionContext, CancellationToken, ValueTask>? CreateTestSessionHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = BuildHookParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await HandleHookResult(result);
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method parameter does not satisfy annotation requirements")]
    private Func<BeforeTestDiscoveryContext, CancellationToken, ValueTask>? CreateBeforeTestDiscoveryHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = BuildHookParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await HandleHookResult(result);
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method parameter does not satisfy annotation requirements")]
    private Func<TestDiscoveryContext, CancellationToken, ValueTask>? CreateAfterTestDiscoveryHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = BuildHookParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await HandleHookResult(result);
        };
    }

    private object?[] BuildHookParameters(MethodInfo method, object context, CancellationToken cancellationToken)
    {
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            if (paramType == typeof(CancellationToken))
            {
                args[i] = cancellationToken;
            }
            else if (paramType == context.GetType())
            {
                args[i] = context;
            }
            else if (context is TestContext testContext)
            {
                if (paramType == typeof(ClassHookContext))
                {
                    args[i] = testContext.ClassContext;
                }
                else if (paramType == typeof(AssemblyHookContext))
                {
                    args[i] = testContext.ClassContext.AssemblyContext;
                }
                else if (paramType == typeof(TestSessionContext))
                {
                    args[i] = testContext.ClassContext.AssemblyContext.TestSessionContext;
                }
            }
            else if (context is ClassHookContext classContext)
            {
                if (paramType == typeof(AssemblyHookContext))
                {
                    args[i] = classContext.AssemblyContext;
                }
                else if (paramType == typeof(TestSessionContext))
                {
                    args[i] = classContext.AssemblyContext.TestSessionContext;
                }
            }
            else if (context is AssemblyHookContext assemblyContext)
            {
                if (paramType == typeof(TestSessionContext))
                {
                    args[i] = assemblyContext.TestSessionContext;
                }
            }
        }

        return args;
    }

    private static async Task HandleHookResult(object? result)
    {
        if (result is Task task)
        {
            await task;
        }
        else if (result is ValueTask valueTask)
        {
            await valueTask;
        }
    }

    private static bool IsCompilerGenerated(Type type)
    {
        return type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
    }
}