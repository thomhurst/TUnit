using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Engine.Discovery;
using TUnit.Engine.Helpers;

namespace TUnit.Engine;

/// <summary>
/// Per-session TUnit initialization. Delegates process-wide setup (exception handlers,
/// parameter parsing) to <see cref="TUnitProcessInitializer"/> and performs session-scoped
/// work (hook discovery, working directory) at most once per <see cref="TUnitServiceProvider"/>
/// instance.
/// </summary>
internal class TUnitInitializer
{
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly IHookRegistrar _hookDiscoveryService;
    private readonly OneTimeGate _sessionGate = new(
        contextName: nameof(TUnitInitializer),
        waitTimeout: TimeSpan.FromMinutes(5));

    public TUnitInitializer(ICommandLineOptions commandLineOptions, IHookRegistrar hookDiscoveryService)
    {
        _commandLineOptions = commandLineOptions;
        _hookDiscoveryService = hookDiscoveryService;
    }

    public void Initialize()
    {
        TUnitProcessInitializer.EnsureInitialised(_commandLineOptions);

        // Session-scoped: every concurrent ExecuteRequestAsync call on this session blocks
        // until hook discovery + working directory setup have finished, so no caller proceeds
        // past partial state. Cross-session "run once per process" semantics for reflection
        // hook discovery live downstream in ReflectionHookDiscoveryService.
        _sessionGate.Run(() =>
        {
            _hookDiscoveryService.DiscoverHooks();

            if (!string.IsNullOrEmpty(TestContext.OutputDirectory))
            {
                TestContext.WorkingDirectory = TestContext.OutputDirectory!;
            }
        });
    }
}
