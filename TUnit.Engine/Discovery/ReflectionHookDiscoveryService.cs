using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Discovers hooks at runtime using reflection for non-source-generated scenarios
/// </summary>
[RequiresUnreferencedCode("Reflection-based hook discovery requires unreferenced code")]
[RequiresDynamicCode("Expression compilation requires dynamic code generation")]
internal sealed class ReflectionHookDiscoveryService
{
    private static readonly ConcurrentDictionary<Assembly, bool> _scannedAssemblies = new();

    /// <summary>
    /// Discovers and registers hooks from all relevant assemblies
    /// </summary>
    /// <param name="assemblies"></param>
    public static void DiscoverAndRegisterHooks(List<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            if (!_scannedAssemblies.TryAdd(assembly, true))
            {
                continue; // Already scanned
            }

            try
            {
                DiscoverHooksInAssembly(assembly);
            }
            catch (Exception)
            {
                // Log error but continue with other assemblies
                // Errors in hook discovery should not prevent test execution
            }
        }
    }

    private static void DiscoverHooksInAssembly(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException rtle)
        {
            // Some types might fail to load, but we can still use the ones that loaded successfully
            types = rtle.Types.Where(t => t != null).Cast<Type>().ToArray();
        }
        catch (Exception)
        {
            return; // Skip this assembly
        }

        foreach (var type in types.Where(t => t.IsClass && !IsCompilerGenerated(t)))
        {
            // Skip abstract types - they can't be instantiated
            if (type.IsAbstract)
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
            // Check for Before attributes
            var beforeAttributes = method.GetCustomAttributes<BeforeAttribute>(false);
            foreach (var beforeAttr in beforeAttributes)
            {
                RegisterHook(type, method, beforeAttr, "Before");
            }

            // Check for After attributes
            var afterAttributes = method.GetCustomAttributes<AfterAttribute>(false);
            foreach (var afterAttr in afterAttributes)
            {
                RegisterHook(type, method, afterAttr, "After");
            }

            // Check for BeforeEvery attributes
            var beforeEveryAttributes = method.GetCustomAttributes<BeforeEveryAttribute>(false);
            foreach (var beforeEveryAttr in beforeEveryAttributes)
            {
                RegisterHook(type, method, beforeEveryAttr, "BeforeEvery");
            }

            // Check for AfterEvery attributes
            var afterEveryAttributes = method.GetCustomAttributes<AfterEveryAttribute>(false);
            foreach (var afterEveryAttr in afterEveryAttributes)
            {
                RegisterHook(type, method, afterEveryAttr, "AfterEvery");
            }
        }
    }

    private static void RegisterHook(Type type, MethodInfo method, HookAttribute hookAttribute, string hookKind)
    {
        if (!IsValidHookMethod(method, hookAttribute.HookType))
        {
            return;
        }

        var hookType = hookAttribute.HookType;
        var order = hookAttribute.Order;
        var methodMetadata = ReflectionMetadataBuilder.CreateMethodMetadata(type, method);
        var hookExecutor = GetHookExecutor();

        switch (hookType)
        {
            case HookType.Test when hookKind is "Before" or "After":
                RegisterInstanceHook(type, method, hookKind, order, methodMetadata, hookExecutor);
                break;

            case HookType.Test when hookKind is "BeforeEvery":
                RegisterGlobalTestHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: true);
                break;

            case HookType.Test when hookKind is "AfterEvery":
                RegisterGlobalTestHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: false);
                break;

            case HookType.Class when hookKind is "Before" or "After":
                RegisterClassHook(type, method, hookKind, order, methodMetadata, hookExecutor);
                break;

            case HookType.Class when hookKind is "BeforeEvery":
                RegisterGlobalClassHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: true);
                break;

            case HookType.Class when hookKind is "AfterEvery":
                RegisterGlobalClassHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: false);
                break;

            case HookType.Assembly when hookKind is "Before" or "After":
                RegisterAssemblyHook(type.Assembly, method, hookKind, order, methodMetadata, hookExecutor);
                break;

            case HookType.Assembly when hookKind is "BeforeEvery":
                RegisterGlobalAssemblyHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: true);
                break;

            case HookType.Assembly when hookKind is "AfterEvery":
                RegisterGlobalAssemblyHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: false);
                break;

            case HookType.TestSession when hookKind is "Before" or "BeforeEvery":
                RegisterTestSessionHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: true);
                break;

            case HookType.TestSession when hookKind is "After" or "AfterEvery":
                RegisterTestSessionHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: false);
                break;

            case HookType.TestDiscovery when hookKind is "Before" or "BeforeEvery":
                RegisterTestDiscoveryHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: true);
                break;

            case HookType.TestDiscovery when hookKind is "After" or "AfterEvery":
                RegisterTestDiscoveryHook(method, methodMetadata, hookExecutor, order, isBeforeEvery: false);
                break;
        }
    }

    private static void RegisterInstanceHook(Type type, MethodInfo method, string hookKind, int order,
        MethodMetadata methodMetadata, IHookExecutor hookExecutor)
    {
        var registrationIndex = hookKind == "Before"
            ? HookRegistrationIndices.GetNextBeforeTestHookIndex()
            : HookRegistrationIndices.GetNextAfterTestHookIndex();

        var instanceHook = new InstanceHookMethod
        {
            InitClassType = type,
            MethodInfo = methodMetadata,
            HookExecutor = hookExecutor,
            Order = order,
            RegistrationIndex = registrationIndex,
            Body = CreateInstanceHookBody(method)
        };

        var collection = hookKind == "Before" ? Sources.BeforeTestHooks : Sources.AfterTestHooks;
        collection.GetOrAdd(type, _ => new ConcurrentBag<InstanceHookMethod>()).Add(instanceHook);
    }

    private static void RegisterGlobalTestHook(MethodInfo method, MethodMetadata methodMetadata,
        IHookExecutor hookExecutor, int order, bool isBeforeEvery)
    {
        var registrationIndex = isBeforeEvery
            ? HookRegistrationIndices.GetNextBeforeEveryTestHookIndex()
            : HookRegistrationIndices.GetNextAfterEveryTestHookIndex();

        var hookMethod = new BeforeTestHookMethod
        {
            MethodInfo = methodMetadata,
            HookExecutor = hookExecutor,
            Order = order,
            RegistrationIndex = registrationIndex,
            Body = CreateStaticTestHookBody(method),
            FilePath = GetFilePath(method),
            LineNumber = GetLineNumber(method)
        };

        // Register hooks in appropriate collections
        if (isBeforeEvery)
        {
            Sources.BeforeEveryTestHooks.Add(hookMethod);
        }
        else
        {
            Sources.AfterEveryTestHooks.Add(new AfterTestHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticTestHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            });
        }
    }

    private static void RegisterClassHook(Type type, MethodInfo method, string hookKind, int order,
        MethodMetadata methodMetadata, IHookExecutor hookExecutor)
    {
        var registrationIndex = hookKind == "Before"
            ? HookRegistrationIndices.GetNextBeforeClassHookIndex()
            : HookRegistrationIndices.GetNextAfterClassHookIndex();

        var hookMethod = hookKind == "Before"
            ? new BeforeClassHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticClassHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            }
            : (StaticHookMethod<ClassHookContext>)new AfterClassHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticClassHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };

        if (hookKind == "Before")
        {
            Sources.BeforeClassHooks.GetOrAdd(type, _ => new ConcurrentBag<BeforeClassHookMethod>()).Add((BeforeClassHookMethod)hookMethod);
        }
        else
        {
            Sources.AfterClassHooks.GetOrAdd(type, _ => new ConcurrentBag<AfterClassHookMethod>()).Add((AfterClassHookMethod)hookMethod);
        }
    }

    private static void RegisterGlobalClassHook(MethodInfo method, MethodMetadata methodMetadata,
        IHookExecutor hookExecutor, int order, bool isBeforeEvery)
    {
        var registrationIndex = isBeforeEvery
            ? HookRegistrationIndices.GetNextBeforeEveryClassHookIndex()
            : HookRegistrationIndices.GetNextAfterEveryClassHookIndex();

        if (isBeforeEvery)
        {
            var hookMethod = new BeforeClassHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticClassHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };
            Sources.BeforeEveryClassHooks.Add(hookMethod);
        }
        else
        {
            var hookMethod = new AfterClassHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticClassHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };
            Sources.AfterEveryClassHooks.Add(hookMethod);
        }
    }

    private static void RegisterAssemblyHook(Assembly assembly, MethodInfo method, string hookKind, int order,
        MethodMetadata methodMetadata, IHookExecutor hookExecutor)
    {
        var registrationIndex = hookKind == "Before"
            ? HookRegistrationIndices.GetNextBeforeAssemblyHookIndex()
            : HookRegistrationIndices.GetNextAfterAssemblyHookIndex();

        var hookMethod = hookKind == "Before"
            ? new BeforeAssemblyHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticAssemblyHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            }
            : (StaticHookMethod<AssemblyHookContext>)new AfterAssemblyHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticAssemblyHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };

        if (hookKind == "Before")
        {
            Sources.BeforeAssemblyHooks.GetOrAdd(assembly, _ => new ConcurrentBag<BeforeAssemblyHookMethod>()).Add((BeforeAssemblyHookMethod)hookMethod);
        }
        else
        {
            Sources.AfterAssemblyHooks.GetOrAdd(assembly, _ => new ConcurrentBag<AfterAssemblyHookMethod>()).Add((AfterAssemblyHookMethod)hookMethod);
        }
    }

    private static void RegisterGlobalAssemblyHook(MethodInfo method, MethodMetadata methodMetadata,
        IHookExecutor hookExecutor, int order, bool isBeforeEvery)
    {
        var registrationIndex = isBeforeEvery
            ? HookRegistrationIndices.GetNextBeforeEveryAssemblyHookIndex()
            : HookRegistrationIndices.GetNextAfterEveryAssemblyHookIndex();

        if (isBeforeEvery)
        {
            var hookMethod = new BeforeAssemblyHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticAssemblyHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };
            Sources.BeforeEveryAssemblyHooks.Add(hookMethod);
        }
        else
        {
            var hookMethod = new AfterAssemblyHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticAssemblyHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };
            Sources.AfterEveryAssemblyHooks.Add(hookMethod);
        }
    }

    private static void RegisterTestSessionHook(MethodInfo method, MethodMetadata methodMetadata,
        IHookExecutor hookExecutor, int order, bool isBeforeEvery)
    {
        var registrationIndex = isBeforeEvery
            ? HookRegistrationIndices.GetNextBeforeTestSessionHookIndex()
            : HookRegistrationIndices.GetNextAfterTestSessionHookIndex();

        if (isBeforeEvery)
        {
            var hookMethod = new BeforeTestSessionHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticTestSessionHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };
            Sources.BeforeTestSessionHooks.Add(hookMethod);
        }
        else
        {
            var hookMethod = new AfterTestSessionHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateStaticTestSessionHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };
            Sources.AfterTestSessionHooks.Add(hookMethod);
        }
    }

    private static void RegisterTestDiscoveryHook(MethodInfo method, MethodMetadata methodMetadata,
        IHookExecutor hookExecutor, int order, bool isBeforeEvery)
    {
        var registrationIndex = isBeforeEvery
            ? HookRegistrationIndices.GetNextBeforeTestDiscoveryHookIndex()
            : HookRegistrationIndices.GetNextAfterTestDiscoveryHookIndex();

        if (isBeforeEvery)
        {
            var hookMethod = new BeforeTestDiscoveryHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateBeforeTestDiscoveryHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };
            Sources.BeforeTestDiscoveryHooks.Add(hookMethod);
        }
        else
        {
            var hookMethod = new AfterTestDiscoveryHookMethod
            {
                MethodInfo = methodMetadata,
                HookExecutor = hookExecutor,
                Order = order,
                RegistrationIndex = registrationIndex,
                Body = CreateAfterTestDiscoveryHookBody(method),
                FilePath = GetFilePath(method),
                LineNumber = GetLineNumber(method)
            };
            Sources.AfterTestDiscoveryHooks.Add(hookMethod);
        }
    }

    private static bool IsValidHookMethod(MethodInfo method, HookType hookType)
    {
        // Check return type
        var returnType = method.ReturnType;
        if (returnType != typeof(void) &&
            returnType != typeof(Task) &&
            returnType != typeof(ValueTask))
        {
            return false;
        }

        var parameters = method.GetParameters();

        // Check parameter count
        if (parameters.Length > 2)
        {
            return false;
        }

        // Validate parameters based on hook type
        if (parameters.Length >= 1)
        {
            var firstParam = parameters[0];
            var firstParamType = firstParam.ParameterType;

            if (firstParamType == typeof(CancellationToken))
            {
                return parameters.Length == 1;
            }

            var expectedContextType = hookType switch
            {
                HookType.Test => typeof(TestContext),
                HookType.Class => typeof(ClassHookContext),
                HookType.Assembly => typeof(AssemblyHookContext),
                HookType.TestSession => typeof(TestSessionContext),
                HookType.TestDiscovery => typeof(BeforeTestDiscoveryContext), // Will be adjusted in actual validation
                _ => null
            };

            if (expectedContextType == null || !expectedContextType.IsAssignableFrom(firstParamType))
            {
                return false;
            }

            if (parameters.Length == 2)
            {
                var secondParam = parameters[1];
                return secondParam.ParameterType == typeof(CancellationToken);
            }
        }

        return true;
    }

    private static Func<object, TestContext, CancellationToken, ValueTask> CreateInstanceHookBody(MethodInfo method)
    {
        return async (instance, context, cancellationToken) =>
        {
            var parameters = GetMethodParameters(method, context, cancellationToken);
            var result = method.Invoke(instance, parameters);
            await ConvertToValueTask(result);
        };
    }

    private static Func<TestContext, CancellationToken, ValueTask> CreateStaticTestHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = GetMethodParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await ConvertToValueTask(result);
        };
    }

    private static Func<ClassHookContext, CancellationToken, ValueTask> CreateStaticClassHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = GetMethodParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await ConvertToValueTask(result);
        };
    }

    private static Func<AssemblyHookContext, CancellationToken, ValueTask> CreateStaticAssemblyHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = GetMethodParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await ConvertToValueTask(result);
        };
    }

    private static Func<TestSessionContext, CancellationToken, ValueTask> CreateStaticTestSessionHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = GetMethodParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await ConvertToValueTask(result);
        };
    }

    private static Func<BeforeTestDiscoveryContext, CancellationToken, ValueTask> CreateBeforeTestDiscoveryHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = GetMethodParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await ConvertToValueTask(result);
        };
    }

    private static Func<TestDiscoveryContext, CancellationToken, ValueTask> CreateAfterTestDiscoveryHookBody(MethodInfo method)
    {
        return async (context, cancellationToken) =>
        {
            var parameters = GetMethodParameters(method, context, cancellationToken);
            var result = method.Invoke(null, parameters);
            await ConvertToValueTask(result);
        };
    }

    private static object?[] GetMethodParameters(MethodInfo method, object context, CancellationToken cancellationToken)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return Array.Empty<object>();
        }

        var args = new object?[parameters.Length];
        var contextSet = false;

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            if (paramType == typeof(CancellationToken))
            {
                args[i] = cancellationToken;
            }
            else if (!contextSet && paramType.IsInstanceOfType(context))
            {
                args[i] = context;
                contextSet = true;
            }
            else
            {
                args[i] = null;
            }
        }

        return args;
    }

    private static async ValueTask ConvertToValueTask(object? result)
    {
        switch (result)
        {
            case null:
                return;
            case Task task:
                await task;
                break;
            case ValueTask valueTask:
                await valueTask;
                break;
        }
    }

    private static IHookExecutor GetHookExecutor()
    {
        // Use the default hook executor
        return DefaultExecutor.Instance;
    }

    private static string GetFilePath(MethodInfo method)
    {
        // In reflection mode, we don't have access to file paths
        return "Unknown";
    }

    private static int GetLineNumber(MethodInfo method)
    {
        // In reflection mode, we don't have access to line numbers
        return 0;
    }

    private static bool IsCompilerGenerated(Type type)
    {
        return type.IsDefined(typeof(CompilerGeneratedAttribute), false);
    }

    private static bool ShouldScanAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name == null)
        {
            return false;
        }

        // Use the same assembly filtering logic as ReflectionTestDataCollector
        var excludedNames = new HashSet<string>
        {
            "mscorlib", "System", "System.Core", "System.Runtime", "System.Private.CoreLib",
            "System.Collections", "System.Linq", "System.Threading", "System.Text.RegularExpressions",
            "Microsoft.CSharp", "Microsoft.Win32.Primitives", "Microsoft.VisualBasic.Core",
            "TUnit", "TUnit.Core", "TUnit.Engine", "TUnit.Assertions",
            "testhost", "Microsoft.TestPlatform.CoreUtilities", "Microsoft.Testing.Platform"
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

        // Check if the assembly references TUnit
        return assembly.GetReferencedAssemblies().Any(a =>
            a.Name != null && (a.Name.StartsWith("TUnit") || a.Name == "TUnit"));
    }
}
