using System.Diagnostics;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Exceptions;

namespace TUnit.Engine;

internal class TUnitInitializer(ICommandLineOptions commandLineOptions)
{
    public void Initialize(ExecuteRequestContext context)
    {
        ConfigureGlobalExceptionHandlers(context);
        SetUpExceptionListeners();
        ParseParameters();

        if (!string.IsNullOrEmpty(TestContext.OutputDirectory))
        {
            TestContext.WorkingDirectory = TestContext.OutputDirectory!;
        }
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
            TestContext.InternalParametersDictionary.Add(split[0], split[1]);
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
