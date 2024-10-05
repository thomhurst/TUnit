using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;

namespace TUnit.Engine;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal class StaticPropertyInjectorsOrchestrator
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly Disposer _disposer;

    public StaticPropertyInjectorsOrchestrator(TUnitFrameworkLogger logger, Disposer disposer)
    {
        _logger = logger;
        _disposer = disposer;
    }
    
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

    public async Task DisposeAll()
    {
        foreach (var staticInjectedProperty in TestDictionary.StaticInjectedProperties.Values)
        {
            try
            {
                var obj = staticInjectedProperty();
                await _disposer.DisposeAsync(obj);
            }
            catch (Exception e)
            {
                await _logger.LogErrorAsync(e);
            }
        }
    }
}