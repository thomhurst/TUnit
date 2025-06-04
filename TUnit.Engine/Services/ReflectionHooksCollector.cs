﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Exceptions;
using TUnit.Core.Executors;
using TUnit.Core.Helpers;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Services;

[UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
[UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
internal class ReflectionHooksCollector(string sessionId) : HooksCollectorBase(sessionId)
{
    private static BindingFlags BindingFlags => 
        BindingFlags.Public 
        | BindingFlags.NonPublic 
        | BindingFlags.Instance 
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    private bool HasHookType(MethodInfo methodInfo, HookType hookType, [NotNullWhen(true)] out HookAttribute? hookAttribute)
    {
        hookAttribute = methodInfo.GetCustomAttributes()
            .OfType<HookAttribute>()
            .FirstOrDefault(x => IsHookType(x, hookType));
        
        return hookAttribute is not null;
    }

    private static bool IsHookType(Attribute x, HookType hookType)
    {
        return x switch
        {
            BeforeAttribute beforeAttribute when beforeAttribute.HookType == hookType => true,
            BeforeEveryAttribute beforeEveryAttribute when beforeEveryAttribute.HookType == hookType => true,
            AfterAttribute afterAttribute when afterAttribute.HookType == hookType => true,
            _ => x is AfterEveryAttribute afterEveryAttribute && afterEveryAttribute.HookType == hookType
        };
    }

    private static IHookExecutor GetHookExecutor(MethodInfo arg)
    {
        var customHookExecutorType = arg.GetCustomAttribute<HookExecutorAttribute>()?.HookExecutorType;

        if (customHookExecutorType is not null)
        {
            return (IHookExecutor)Activator.CreateInstance(customHookExecutorType)!;
        }

        return DefaultExecutor.Instance;
    }

    private bool IsHook(MethodInfo arg)
    {
        try
        {
            return arg.GetCustomAttributes()
                .OfType<HookAttribute>()
                .Any();
        }
        catch
        {
            return false;
        }
    }

    private protected override List<StaticHookMethod<BeforeTestDiscoveryContext>> CollectBeforeTestDiscoveryHooks()
    {
        var list = new List<StaticHookMethod<BeforeTestDiscoveryContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.TestDiscovery, out var hookAttribute) && hookAttribute is BeforeAttribute or BeforeEveryAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    list.Add(new BeforeTestDiscoveryHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }

    private protected override List<StaticHookMethod<TestSessionContext>> CollectBeforeTestSessionHooks()
    {
        var list = new List<StaticHookMethod<TestSessionContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.TestSession, out var hookAttribute) && hookAttribute is BeforeAttribute or BeforeEveryAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    
                    list.Add(new BeforeTestSessionHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }

    private protected override GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> CollectBeforeAssemblyHooks()
    {
        var dict = new GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Assembly, out var hookAttribute) && hookAttribute is BeforeAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    var assembly = sourceGeneratedMethodInformation.Class.Type.Assembly;
                    
                    dict.GetOrAdd(assembly, _ => []).Add(new BeforeAssemblyHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return dict;
    }

    private protected override GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> CollectBeforeClassHooks()
    {
        var dict = new GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Class, out var hookAttribute) && hookAttribute is BeforeAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    
                    dict.GetOrAdd(type, _ => []).Add(new BeforeClassHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return dict;
    }

    private protected override GetOnlyDictionary<Type, List<InstanceHookMethod>> CollectBeforeTestHooks()
    {
        var dict = new GetOnlyDictionary<Type, List<InstanceHookMethod>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Test, out var hookAttribute) && hookAttribute is BeforeAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                   
                    dict.GetOrAdd(type, _ => []).Add(new InstanceHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        ClassType = type,
                        Body = (instance, context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeInstanceHook(instance, context, token)),
                    });
                }
            }
        }
        return dict;
    }

    private protected override List<StaticHookMethod<AssemblyHookContext>> CollectBeforeEveryAssemblyHooks()
    {
        var list = new List<StaticHookMethod<AssemblyHookContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Assembly, out var hookAttribute) && hookAttribute is BeforeEveryAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    list.Add(new BeforeAssemblyHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }

    private protected override List<StaticHookMethod<ClassHookContext>> CollectBeforeEveryClassHooks()
    {
        var list = new List<StaticHookMethod<ClassHookContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Class, out var hookAttribute) && hookAttribute is BeforeEveryAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    list.Add(new BeforeClassHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }

    private protected override List<StaticHookMethod<TestContext>> CollectBeforeEveryTestHooks()
    {
        var list = new List<StaticHookMethod<TestContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Test, out var hookAttribute) && hookAttribute is BeforeEveryAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    list.Add(new BeforeTestHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }

    private protected override List<StaticHookMethod<TestDiscoveryContext>> CollectAfterTestDiscoveryHooks()
    {
        var list = new List<StaticHookMethod<TestDiscoveryContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.TestDiscovery, out var hookAttribute) && !(hookAttribute is BeforeAttribute or BeforeEveryAttribute))
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    list.Add(new AfterTestDiscoveryHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }

    private protected override List<StaticHookMethod<TestSessionContext>> CollectAfterTestSessionHooks()
    {
        var list = new List<StaticHookMethod<TestSessionContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.TestSession, out var hookAttribute) && !(hookAttribute is BeforeAttribute or BeforeEveryAttribute))
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    list.Add(new AfterTestSessionHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }

    private protected override GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>> CollectAfterAssemblyHooks()
    {
        var dict = new GetOnlyDictionary<Assembly, List<StaticHookMethod<AssemblyHookContext>>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Assembly, out var hookAttribute) && hookAttribute is AfterAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    var assembly = sourceGeneratedMethodInformation.Class.Type.Assembly;
                    
                    dict.GetOrAdd(assembly, _ => []).Add(new AfterAssemblyHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return dict;
    }

    private protected override GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>> CollectAfterClassHooks()
    {
        var dict = new GetOnlyDictionary<Type, List<StaticHookMethod<ClassHookContext>>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Class, out var hookAttribute) && hookAttribute is AfterAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    
                    dict.GetOrAdd(type, _ => []).Add(new AfterClassHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return dict;
    }

    private protected override GetOnlyDictionary<Type, List<InstanceHookMethod>> CollectAfterTestHooks()
    {
        var dict = new GetOnlyDictionary<Type, List<InstanceHookMethod>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Test, out var hookAttribute) && hookAttribute is AfterAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    
                    dict.GetOrAdd(type, _ => []).Add(new InstanceHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        ClassType = type,
                        Body = (instance, context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeInstanceHook(instance, context, token)),
                    });
                }
            }
        }
        return dict;
    }

    private protected override List<StaticHookMethod<AssemblyHookContext>> CollectAfterEveryAssemblyHooks()
    {
        var list = new List<StaticHookMethod<AssemblyHookContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Assembly, out var hookAttribute) && hookAttribute is AfterEveryAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    list.Add(new AfterAssemblyHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }

    private protected override List<StaticHookMethod<ClassHookContext>> CollectAfterEveryClassHooks()
    {
        var list = new List<StaticHookMethod<ClassHookContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Class, out var hookAttribute) && hookAttribute is AfterEveryAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    list.Add(new AfterClassHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }

    private protected override List<StaticHookMethod<TestContext>> CollectAfterEveryTestHooks()
    {
        var list = new List<StaticHookMethod<TestContext>>();
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.Test, out var hookAttribute) && hookAttribute is AfterEveryAttribute)
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);
                    list.Add(new AfterTestHookMethod
                    {
                        MethodInfo = sourceGeneratedMethodInformation,
                        Order = hookAttribute.Order,
                        HookExecutor = GetHookExecutor(methodInfo),
                        FilePath = hookAttribute.File,
                        LineNumber = hookAttribute.Line,
                        Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                    });
                }
            }
        }
        return list;
    }
}

