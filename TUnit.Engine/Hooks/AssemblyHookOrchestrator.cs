using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Models;
using TUnit.Engine.Data;

namespace TUnit.Engine.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class AssemblyHookOrchestrator
{
    private static readonly ConcurrentDictionary<Assembly, AssemblyHookContext> AssemblyHookContexts = new();
    
    private static readonly GetOnlyDictionary<Assembly, List<Lazy<Task>>> SetUps = new();
    private static readonly GetOnlyDictionary<Assembly, List<Lazy<Task>>> CleanUps = new();
    
    public static void RegisterSetUp(StaticMethod staticMethod)
    {
        var setups = SetUps.GetOrAdd(staticMethod.MethodInfo.ReflectedType!.Assembly, _ => []);
        setups.Add(Convert(staticMethod));
    }

    public static void RegisterCleanUp(StaticMethod staticMethod)
    {
        var cleanups = CleanUps.GetOrAdd(staticMethod.MethodInfo.ReflectedType!.Assembly, _ => []);
        cleanups.Add(Convert(staticMethod));
    }
    
    public static IEnumerable<AssemblyHookContext> GetAllAssemblyHookContexts() => AssemblyHookContexts.Values;
    
    public static void RegisterTestContext(Assembly assembly, ClassHookContext classHookContext)
    {
        var assemblyHookContext = AssemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext
        {
            Assembly = assembly
        });

        assemblyHookContext.TestClasses.Add(classHookContext);
    }
    
    public static AssemblyHookContext GetAssemblyHookContext(Type type)
    {
        lock (type)
        {
            return AssemblyHookContexts.GetOrAdd(type.Assembly, _ => new AssemblyHookContext
            {
                Assembly = type.Assembly
            });
        }
    }

    private static Lazy<Task> Convert(StaticMethod staticMethod)
    {
        return new Lazy<Task>(() =>
        {
            var timeout = staticMethod.Timeout;
            
            return RunHelpers.RunWithTimeoutAsync(token => staticMethod.Body(token), timeout);
        });
    }

    public static async Task ExecuteSetups(Assembly assembly)
    {
        foreach (var setUp in SetUps.GetOrAdd(assembly, _ => []))
        {
            await setUp.Value;
        }
    }

    public static async Task ExecuteCleanups(Assembly assembly, List<Exception> exceptions)
    {
        if (!InstanceTracker.IsLastTestForAssembly(assembly))
        {
            return;
        }
        
        foreach (var cleanUp in CleanUps.GetOrAdd(assembly, _ => []))
        {
            try
            {
                await cleanUp.Value;
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }
    }
}