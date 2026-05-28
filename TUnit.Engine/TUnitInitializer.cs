using System.Diagnostics;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Discovery;
using TUnit.Engine.Exceptions;

namespace TUnit.Engine;

internal class TUnitInitializer(ICommandLineOptions commandLineOptions, IHookRegistrar hookDiscoveryService)
{
    // Process-wide guards. Trace listeners and AppDomain handlers must only be registered once
    // per process, regardless of how many sessions or concurrent ExecuteRequestAsync calls hit
    // this initializer — otherwise listeners accumulate per RPC and AppDomain handlers capture
    // stale ExecuteRequestContext closures.
    private static int s_processHandlersRegistered;
    private static int s_parametersParsed;

    private int _sessionInitialised;

    public void Initialize(ExecuteRequestContext context)
    {
        _ = context;

        EnsureProcessHandlersRegistered();
        EnsureParametersParsed();

        if (Interlocked.CompareExchange(ref _sessionInitialised, 1, 0) == 0)
        {
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

    private static void EnsureProcessHandlersRegistered()
    {
        if (Interlocked.CompareExchange(ref s_processHandlersRegistered, 1, 0) != 0)
        {
            return;
        }

        Trace.Listeners.Insert(0, new ThrowListener());

        AppDomain.CurrentDomain.UnhandledException += static (_, args) =>
        {
            // Log only — we deliberately do not capture an ExecuteRequestContext to call
            // Complete() on, because under MTP server mode multiple contexts may be in flight
            // concurrently and capturing any one of them produces use-after-complete bugs and
            // handler leaks. If the process is terminating, MTP's own shutdown path takes over.
            var exception = args.ExceptionObject as Exception;
            Console.Error.WriteLine($"Unhandled exception in AppDomain: {exception}");
        };

        TaskScheduler.UnobservedTaskException += static (_, args) =>
        {
            Console.Error.WriteLine($"Unobserved task exception: {args.Exception}");

            // Mark as observed to prevent process termination
            args.SetObserved();
        };
    }

    private void EnsureParametersParsed()
    {
        if (Interlocked.CompareExchange(ref s_parametersParsed, 1, 0) != 0)
        {
            return;
        }

        if (!commandLineOptions.TryGetOptionArgumentList(ParametersCommandProvider.TestParameter, out var parameters))
        {
            return;
        }

        foreach (var parameter in parameters)
        {
            var split = parameter.Split('=');
            var key = split[0];
            var value = split[1];

            var list = TestContext.InternalParametersDictionary.GetOrAdd(key, static _ => []);
            list.Add(value);
        }
    }
}
