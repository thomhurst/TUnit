using System.Diagnostics;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Exceptions;

namespace TUnit.Engine;

/// <summary>
/// Process-wide TUnit engine setup that must run exactly once per host process —
/// global exception handlers, the throw-on-trace-assert listener, and command-line
/// parameter parsing. Lifted out of the per-session <see cref="TUnitInitializer"/>
/// so the lifetime distinction is enforced at the type level rather than relying on
/// a static flag inside an instance method (#6001).
/// </summary>
internal static class TUnitProcessInitializer
{
    private static int s_initialised;

    /// <summary>
    /// Runs process-wide setup the first time it is invoked. Subsequent calls (from
    /// later sessions or concurrent ExecuteRequestAsync invocations) are no-ops.
    /// </summary>
    public static void EnsureInitialised(ICommandLineOptions commandLineOptions)
    {
        if (Interlocked.CompareExchange(ref s_initialised, 1, 0) != 0)
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

        ParseTestParameters(commandLineOptions);
    }

    private static void ParseTestParameters(ICommandLineOptions commandLineOptions)
    {
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
