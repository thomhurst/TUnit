using TUnit.Core;

namespace TUnit.Engine.Services;

internal class SourceGeneratedHooksCollector(string sessionId) : HooksCollectorBase(sessionId)
{
    public override void CollectHooks()
    {
        while (Sources.TestDiscoveryHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestDiscoveryHooks(SessionId))
            {
                BeforeTestDiscoveryHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestDiscoveryHooks(SessionId))
            {
                AfterTestDiscoveryHooks.Add(afterHook);
            }
        }
    
        while (Sources.TestSessionHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestSessionHooks(SessionId))
            {
                BeforeTestSessionHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestSessionHooks(SessionId))
            {
                AfterTestSessionHooks.Add(afterHook);
            }
        }
    
        while (Sources.TestHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeTestHooks(SessionId))
            {
                var beforeList = BeforeTestHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterTestHooks(SessionId))
            {
                var afterList = AfterTestHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryTestHooks(SessionId))
            {
                BeforeEveryTestHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryTestHooks(SessionId))
            {
                AfterEveryTestHooks.Add(afterHook);
            }
        }

        while (Sources.ClassHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeClassHooks(SessionId))
            {
                var beforeList = BeforeClassHooks.GetOrAdd(beforeHook.ClassType, _ => []);
                beforeList.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterClassHooks(SessionId))
            {
                var afterList = AfterClassHooks.GetOrAdd(afterHook.ClassType, _ => []);
                afterList.Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryClassHooks(SessionId))
            {
                BeforeEveryClassHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryClassHooks(SessionId))
            {
                AfterEveryClassHooks.Add(afterHook);
            }
        }

        while (Sources.AssemblyHookSources.TryDequeue(out var hookSource))
        {
            foreach (var beforeHook in hookSource.CollectBeforeAssemblyHooks(SessionId))
            {
                BeforeAssemblyHooks.GetOrAdd(beforeHook.Assembly, _ => []).Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterAssemblyHooks(SessionId))
            {
                AfterAssemblyHooks.GetOrAdd(afterHook.Assembly, _ => []).Add(afterHook);
            }
            
            foreach (var beforeHook in hookSource.CollectBeforeEveryAssemblyHooks(SessionId))
            {
                BeforeEveryAssemblyHooks.Add(beforeHook);
            }

            foreach (var afterHook in hookSource.CollectAfterEveryAssemblyHooks(SessionId))
            {
                AfterEveryAssemblyHooks.Add(afterHook);
            }
        }
    }
}