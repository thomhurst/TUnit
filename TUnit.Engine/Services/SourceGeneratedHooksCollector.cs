using System.Reflection;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Engine.Services;

internal class SourceGeneratedHooksCollector(string sessionId) : HooksCollectorBase(sessionId)
{
    public override void CollectDiscoveryHooks()
    {
        while (Sources.TestDiscoveryHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestDiscoveryHooks(sessionId))
            {
                BeforeTestDiscoveryHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestDiscoveryHooks(sessionId))
            {
                AfterTestDiscoveryHooks.Add(afterHook);
            }
        }
    }

    public override void CollectionTestSessionHooks()
    {
        while (Sources.TestSessionHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestSessionHooks(sessionId))
            {
                BeforeTestSessionHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestSessionHooks(sessionId))
            {
                AfterTestSessionHooks.Add(afterHook);
            }
        }
    }

    public override void CollectHooks()
    {
        while (Sources.TestHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestHooks(sessionId))
            {
                var beforeList = BeforeTestHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestHooks(sessionId))
            {
                var afterList = AfterTestHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryTestHooks(sessionId))
            {
                BeforeEveryTestHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryTestHooks(sessionId))
            {
                AfterEveryTestHooks.Add(afterHook);
            }
        }

        while (Sources.ClassHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeClassHooks(sessionId))
            {
                var beforeList = BeforeClassHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterClassHooks(sessionId))
            {
                var afterList = AfterClassHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryClassHooks(sessionId))
            {
                BeforeEveryClassHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryClassHooks(sessionId))
            {
                AfterEveryClassHooks.Add(afterHook);
            }
        }

        while (Sources.AssemblyHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeAssemblyHooks(sessionId))
            {
                BeforeAssemblyHooks.GetOrAdd(beforeHook.Assembly, _ => []).Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterAssemblyHooks(sessionId))
            {
                AfterAssemblyHooks.GetOrAdd(afterHook.Assembly, _ => []).Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryAssemblyHooks(sessionId))
            {
                BeforeEveryAssemblyHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryAssemblyHooks(sessionId))
            {
                AfterEveryAssemblyHooks.Add(afterHook);
            }
        }
    }
}