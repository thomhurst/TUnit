using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.Discovery;

namespace TUnit.Engine;

/// <summary>
/// Per-session TUnit initialization. Delegates process-wide setup (exception handlers,
/// parameter parsing) to <see cref="TUnitProcessInitializer"/> and performs session-scoped
/// work (hook discovery, working directory) at most once per <see cref="TUnitServiceProvider"/>
/// instance.
/// </summary>
internal class TUnitInitializer(ICommandLineOptions commandLineOptions, IHookRegistrar hookDiscoveryService)
{
    private int _sessionInitialised;

    public void Initialize(ExecuteRequestContext context)
    {
        _ = context;

        TUnitProcessInitializer.EnsureInitialised(commandLineOptions);

        if (Interlocked.CompareExchange(ref _sessionInitialised, 1, 0) != 0)
        {
            return;
        }

        // _sessionInitialised is per-instance — TUnitServiceProvider news a fresh
        // TUnitInitializer per session, so this flag only protects against concurrent
        // ExecuteRequestAsync calls within ONE session re-entering hook discovery.
        // Cross-session "run once per process" semantics live downstream in
        // ReflectionHookDiscoveryService (gate on _discoveryStarted / _discoveryCompleted).
        hookDiscoveryService.DiscoverHooks();

        if (!string.IsNullOrEmpty(TestContext.OutputDirectory))
        {
            TestContext.WorkingDirectory = TestContext.OutputDirectory!;
        }
    }
}
