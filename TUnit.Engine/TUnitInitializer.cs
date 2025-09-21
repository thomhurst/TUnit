using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Discovery;
using TUnit.Engine.Exceptions;

namespace TUnit.Engine;

internal class TUnitInitializer(ICommandLineOptions commandLineOptions)
{
    public void Initialize(ExecuteRequestContext context)
    {
        ConfigureGlobalExceptionHandlers(context);
        SetUpExceptionListeners();
        ParseParameters();

        // Discover hooks via reflection if in reflection mode
        if (IsReflectionMode())
        {
            DiscoverHooksViaReflection();
        }

        if (!string.IsNullOrEmpty(TestContext.OutputDirectory))
        {
            TestContext.WorkingDirectory = TestContext.OutputDirectory!;
        }
    }

    private bool IsReflectionMode()
    {
        return !SourceRegistrar.IsEnabled;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    private void DiscoverHooksViaReflection()
    {
        ReflectionHookDiscoveryService.DiscoverHooks();
    }

    private void SetUpExceptionListeners()
    {
        Trace.Listeners.Insert(0, new ThrowListener());
    }

    private void ParseParameters()
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

            if (!TestContext.InternalParametersDictionary.TryGetValue(key, out var list))
            {
                list =
                [
                ];
                TestContext.InternalParametersDictionary[key] = list;
            }
            list.Add(value);
        }
    }

    private static void ConfigureGlobalExceptionHandlers(ExecuteRequestContext context)
    {
        // Handle unhandled exceptions on any thread
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception;

            Console.Error.WriteLine($"Unhandled exception in AppDomain: {exception}");

            // Force exit to prevent hanging
            if (args.IsTerminating)
            {
                context.Complete();
            }
        };

        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Console.Error.WriteLine($"Unobserved task exception: {args.Exception}");

            // Mark as observed to prevent process termination
            args.SetObserved();
        };
    }
}
