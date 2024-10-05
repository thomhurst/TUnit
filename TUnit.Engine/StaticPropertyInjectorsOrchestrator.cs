using TUnit.Core;

namespace TUnit.Engine;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class StaticPropertyInjectorsOrchestrator
{
    public async ValueTask Execute(Type testClassType)
    {
        if (!TestDictionary.StaticPropertyInjectors.TryGetValue(testClassType, out var initialisers))
        {
            return;
        }

        while (initialisers.TryDequeue(out var initialiser))
        {
            initialiser.InitialiserAction.Value.Invoke();
           
            if (TestDataContainer.InjectedSharedGloballyInitializations.TryGetValue(initialiser.InjectableType,
                    out var asyncInitializer))
            {
                await asyncInitializer.Value;
            }
        }
    }
}