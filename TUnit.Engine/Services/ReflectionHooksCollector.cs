﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
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
    
    public override void CollectDiscoveryHooks()
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException("Reflection tests are not supported with AOT or trimming enabled.");
        }
#endif
        
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.TestDiscovery, out var hookAttribute))
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);

                    if (hookAttribute is BeforeAttribute or BeforeEveryAttribute)
                    {
                        BeforeTestDiscoveryHooks.Add(new BeforeTestDiscoveryHookMethod
                        {
                            MethodInfo = sourceGeneratedMethodInformation,
                            Order = hookAttribute.Order,
                            HookExecutor = GetHookExecutor(methodInfo),
                            FilePath = hookAttribute.File,
                            LineNumber = hookAttribute.Line,
                            Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                        });
                    }
                    else
                    {
                        AfterTestDiscoveryHooks.Add(new AfterTestDiscoveryHookMethod
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
        }
    }

    public override void CollectionTestSessionHooks()
    {
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                if (HasHookType(methodInfo, HookType.TestSession, out var hookAttribute))
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);

                    if (hookAttribute is BeforeAttribute or BeforeEveryAttribute)
                    {
                        BeforeTestSessionHooks.Add(new BeforeTestSessionHookMethod
                        {
                            MethodInfo = sourceGeneratedMethodInformation,
                            Order = hookAttribute.Order,
                            HookExecutor = GetHookExecutor(methodInfo),
                            FilePath = hookAttribute.File,
                            LineNumber = hookAttribute.Line,
                            Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
                        });
                    }
                    else
                    {
                        AfterTestSessionHooks.Add(new AfterTestSessionHookMethod
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
        }
    }

    public override void CollectHooks()
    {
        foreach (var type in ReflectionScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags)
                         .Where(x => !x.IsAbstract)
                         .Where(IsHook))
            {
                try
                {
                    var sourceGeneratedMethodInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, methodInfo, methodInfo.Name);

                    if (HasHookType(methodInfo, HookType.Assembly, out var assemblyHookAttribute))
                    {
                        RegisterAssemblyHook(assemblyHookAttribute, sourceGeneratedMethodInformation, methodInfo);
                    }
                
                    if (HasHookType(methodInfo, HookType.Class, out var classHookAttribute))
                    {
                        RegisterClassHook(classHookAttribute, sourceGeneratedMethodInformation, methodInfo);
                    }
                
                    if (HasHookType(methodInfo, HookType.Test, out var testHookAttribute))
                    {
                        RegisterTestHook(testHookAttribute, sourceGeneratedMethodInformation, methodInfo);
                    }
                }
                catch (Exception e)
                {
                    throw new TUnitException($"""
                                               Error collecting hooks for method {methodInfo.Name} in type {type.FullName}
                                               Line: {methodInfo.GetCustomAttribute<HookAttribute>()?.Line}
                                               File: {methodInfo.GetCustomAttribute<HookAttribute>()?.File}
                                               """, e);
                }
            }
        }
    }

    private void RegisterAssemblyHook(HookAttribute hookAttribute, SourceGeneratedMethodInformation sourceGeneratedMethodInformation, MethodInfo methodInfo)
    {
        var assembly = sourceGeneratedMethodInformation.Class.Type.Assembly;
        
        if (hookAttribute is BeforeAttribute)
        {
            BeforeAssemblyHooks.GetOrAdd(assembly, _ => []).Add(new BeforeAssemblyHookMethod
            {
                MethodInfo = sourceGeneratedMethodInformation,
                Order = hookAttribute.Order,
                HookExecutor = GetHookExecutor(methodInfo),
                FilePath = hookAttribute.File,
                LineNumber = hookAttribute.Line,
                Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
            });
        }
        else if (hookAttribute is AfterAttribute)
        {
            AfterAssemblyHooks.GetOrAdd(assembly, _ => []).Add(new AfterAssemblyHookMethod
            {
                MethodInfo = sourceGeneratedMethodInformation,
                Order = hookAttribute.Order,
                HookExecutor = GetHookExecutor(methodInfo),
                FilePath = hookAttribute.File,
                LineNumber = hookAttribute.Line,
                Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
            });
        }
        else if (hookAttribute is BeforeEveryAttribute)
        {
            BeforeEveryAssemblyHooks.Add(new BeforeAssemblyHookMethod
            {
                MethodInfo = sourceGeneratedMethodInformation,
                Order = hookAttribute.Order,
                HookExecutor = GetHookExecutor(methodInfo),
                FilePath = hookAttribute.File,
                LineNumber = hookAttribute.Line,
                Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
            });
        }
        else
        {
            AfterEveryAssemblyHooks.Add(new AfterAssemblyHookMethod
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

    private void RegisterClassHook(HookAttribute hookAttribute, SourceGeneratedMethodInformation sourceGeneratedMethodInformation, MethodInfo methodInfo)
    {
        var type = sourceGeneratedMethodInformation.Class.Type;
        
        if (hookAttribute is BeforeAttribute)
        {
            BeforeClassHooks.GetOrAdd(type, _ => []).Add(new BeforeClassHookMethod
            {
                MethodInfo = sourceGeneratedMethodInformation,
                Order = hookAttribute.Order,
                HookExecutor = GetHookExecutor(methodInfo),
                FilePath = hookAttribute.File,
                LineNumber = hookAttribute.Line,
                Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
            });
        }
        else if (hookAttribute is AfterAttribute)
        {
            AfterClassHooks.GetOrAdd(type, _ => []).Add(new AfterClassHookMethod
            {
                MethodInfo = sourceGeneratedMethodInformation,
                Order = hookAttribute.Order,
                HookExecutor = GetHookExecutor(methodInfo),
                FilePath = hookAttribute.File,
                LineNumber = hookAttribute.Line,
                Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
            });
        }
        else if (hookAttribute is BeforeEveryAttribute)
        {
            BeforeEveryClassHooks.Add(new BeforeClassHookMethod
            {
                MethodInfo = sourceGeneratedMethodInformation,
                Order = hookAttribute.Order,
                HookExecutor = GetHookExecutor(methodInfo),
                FilePath = hookAttribute.File,
                LineNumber = hookAttribute.Line,
                Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
            });
        }
        else
        {
            AfterEveryClassHooks.Add(new AfterClassHookMethod
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

    private void RegisterTestHook(HookAttribute hookAttribute, SourceGeneratedMethodInformation sourceGeneratedMethodInformation, MethodInfo methodInfo)
    {
        var type = sourceGeneratedMethodInformation.Class.Type;
        
        if (hookAttribute is BeforeAttribute)
        {
            BeforeTestHooks.GetOrAdd(type, _ => []).Add(new InstanceHookMethod
            {
                MethodInfo = sourceGeneratedMethodInformation,
                Order = hookAttribute.Order,
                HookExecutor = GetHookExecutor(methodInfo),
                ClassType = type,
                Body = (instance, context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeInstanceHook(instance, context, token)),
            });
        }
        else if (hookAttribute is AfterAttribute)
        {
            AfterTestHooks.GetOrAdd(type, _ => []).Add(new InstanceHookMethod
            {
                MethodInfo = sourceGeneratedMethodInformation,
                Order = hookAttribute.Order,
                HookExecutor = GetHookExecutor(methodInfo),
                ClassType = type,
                Body = (instance, context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeInstanceHook(instance, context, token)),
            });
        }
        else if (hookAttribute is BeforeEveryAttribute)
        {
            BeforeEveryTestHooks.Add(new BeforeTestHookMethod
            {
                MethodInfo = sourceGeneratedMethodInformation,
                Order = hookAttribute.Order,
                HookExecutor = GetHookExecutor(methodInfo),
                FilePath = hookAttribute.File,
                LineNumber = hookAttribute.Line,
                Body = (context, token) => AsyncConvert.ConvertObject(methodInfo.InvokeStaticHook(context, token)),
            });
        }
        else
        {
            AfterEveryTestHooks.Add(new AfterTestHookMethod
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
}