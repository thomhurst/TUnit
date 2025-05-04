using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Executors;
using TUnit.Core.Helpers;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

[SuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[SuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
internal class ReflectionHooksCollector(string sessionId) : HooksCollectorBase(sessionId)
{
    public override void CollectDiscoveryHooks()
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException("Reflection tests are not supported with AOT or trimming enabled.");
        }
#endif
        
        foreach (var type in ReflectionTypeScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods())
            {
                if (HasHookType(methodInfo, HookType.TestDiscovery, out var hookAttribute))
                {
                    var sourceGeneratedMethodInformation = SourceModelHelpers.BuildTestMethod(type, methodInfo, [], methodInfo.Name);

                    if (hookAttribute is BeforeAttribute or BeforeEveryAttribute)
                    {
                        BeforeTestDiscoveryHooks.Add(new BeforeTestDiscoveryHookMethod
                        {
                            MethodInfo = sourceGeneratedMethodInformation,
                            Order = 0,
                            HookExecutor = GetHookExecutor(methodInfo),
                            FilePath = hookAttribute.File,
                            LineNumber = hookAttribute.Line,
                            Body = (context, token) => (ValueTask) methodInfo.Invoke(type, [context, token])!,
                        });
                    }
                    else
                    {
                        AfterTestDiscoveryHooks.Add(new AfterTestDiscoveryHookMethod
                        {
                            MethodInfo = sourceGeneratedMethodInformation,
                            Order = 0,
                            HookExecutor = GetHookExecutor(methodInfo),
                            FilePath = hookAttribute.File,
                            LineNumber = hookAttribute.Line,
                            Body = (context, token) => (ValueTask) methodInfo.Invoke(type, [context, token])!,
                        });
                    }
                }
            }
        }
    }

    public override void CollectionTestSessionHooks()
    {
        foreach (var type in ReflectionTypeScanner.GetTypes())
        {
            foreach (var methodInfo in type.GetMethods())
            {
                if (HasHookType(methodInfo, HookType.TestDiscovery, out var hookAttribute))
                {
                    var sourceGeneratedMethodInformation = SourceModelHelpers.BuildTestMethod(type, methodInfo, [], methodInfo.Name);

                    if (hookAttribute is BeforeAttribute or BeforeEveryAttribute)
                    {
                        BeforeTestSessionHooks.Add(new BeforeTestSessionHookMethod
                        {
                            MethodInfo = sourceGeneratedMethodInformation,
                            Order = 0,
                            HookExecutor = GetHookExecutor(methodInfo),
                            FilePath = hookAttribute.File,
                            LineNumber = hookAttribute.Line,
                            Body = (context, token) => (ValueTask) methodInfo.Invoke(type, [context, token])!,
                        });
                    }
                    else
                    {
                        AfterTestSessionHooks.Add(new AfterTestSessionHookMethod
                        {
                            MethodInfo = sourceGeneratedMethodInformation,
                            Order = 0,
                            HookExecutor = GetHookExecutor(methodInfo),
                            FilePath = hookAttribute.File,
                            LineNumber = hookAttribute.Line,
                            Body = (context, token) => (ValueTask) methodInfo.Invoke(type, [context, token])!,
                        });
                    }
                }
            }
        }
    }

    public override void CollectHooks()
    {
        // TODO: Implement this method
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
}