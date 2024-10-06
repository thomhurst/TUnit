using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Logging;
using TUnit.Engine.Extensions;
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
        foreach (var type in testClassType.GetSelfAndBaseTypes().Reverse())
        {
            if (!TestDictionary.StaticInjectedPropertiesByTestClassType.TryGet(type, out var initialisers)
                || initialisers is null)
            {
                continue;
            }

            while (initialisers.TryDequeue(out var initialiser))
            {
                await initialiser.Value;
            }
        }
    }

    public async Task DisposeAll()
    {
        foreach (var staticInjectedProperty in TestDictionary.StaticInjectedPropertiesByInjectedType.Values)
        {
            try
            {
                var obj = await staticInjectedProperty.Value;
                await _disposer.DisposeAsync(obj);
            }
            catch (Exception e)
            {
                await _logger.LogErrorAsync(e);
            }
        }
    }
}