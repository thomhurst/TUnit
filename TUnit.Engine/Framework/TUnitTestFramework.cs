using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.Services;

namespace TUnit.Engine.Framework;

/// <summary>
/// Unified test framework implementation with proper ExecutionContext handling
/// </summary>
internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly IServiceProvider _frameworkServiceProvider;
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly ConcurrentDictionary<string, TUnitServiceProvider> _serviceProvidersPerSession = new();
    private readonly IRequestHandler _requestHandler;

    public TUnitTestFramework(
        IExtension extension,
        IServiceProvider frameworkServiceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        _extension = extension;
        _frameworkServiceProvider = frameworkServiceProvider;
        _capabilities = capabilities;
        _requestHandler = new TestRequestHandler();

        // Configure Debug/Trace listeners to prevent UI dialogs from blocking test execution
        ConfigureDebugListeners();

        // Set up global exception handlers to prevent hangs
        ConfigureGlobalExceptionHandlers();
    }

    private static void ConfigureDebugListeners()
    {
        // Remove default listeners that can show UI dialogs
        System.Diagnostics.Trace.Listeners.Clear();

        // Add our custom listener that converts assertions to exceptions
        var assertionListener = new Diagnostics.TUnitAssertionListener();
        System.Diagnostics.Trace.Listeners.Add(assertionListener);

        // Configure to not show assert UI dialogs
        System.Diagnostics.Trace.AutoFlush = true;
    }

    private static void ConfigureGlobalExceptionHandlers()
    {
        // Handle unhandled exceptions on any thread
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Console.Error.WriteLine($"Unhandled exception in AppDomain: {exception}");

            // Force exit to prevent hanging
            if (args.IsTerminating)
            {
                Environment.Exit(1);
            }
        };

        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Console.Error.WriteLine($"Unobserved task exception: {args.Exception}");

            // Mark as observed to prevent process termination
            args.SetObserved();
        };
    }

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;
    public Type[] DataTypesProduced => new[] { typeof(TestNodeUpdateMessage) };

    public Task<bool> IsEnabledAsync() => _extension.IsEnabledAsync();

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
    }

    [RequiresDynamicCode("Generic type resolution requires runtime type generation.")]
    [RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming.")]
    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        var serviceProvider = GetOrCreateServiceProvider(context);

        try
        {
            serviceProvider.CancellationToken.Initialise(context.CancellationToken);
            await _requestHandler.HandleRequestAsync((TestExecutionRequest)context.Request, serviceProvider, context);
        }
        catch (Exception e) when (IsCancellationException(e))
        {
            // Check if this is a normal cancellation or fail-fast cancellation
            if (context.CancellationToken.IsCancellationRequested)
            {
                await serviceProvider.Logger.LogErrorAsync("The test run was cancelled.");
            }
            else
            {
                // This is likely a fail-fast cancellation
                await serviceProvider.Logger.LogErrorAsync("Test execution stopped due to fail-fast.");
            }
        }
        catch (Exception e)
        {
            await serviceProvider.Logger.LogErrorAsync(e);
            await ReportUnhandledException(context, e);
        }
        finally
        {
            context.Complete();
        }
    }

    public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        if (_serviceProvidersPerSession.TryRemove(context.SessionUid.Value, out var serviceProvider))
        {
            await serviceProvider.DisposeAsync();
        }

        return new CloseTestSessionResult { IsSuccess = true };
    }

    [RequiresDynamicCode("Generic type resolution requires runtime type generation.")]
    [RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming.")]
    private TUnitServiceProvider GetOrCreateServiceProvider(ExecuteRequestContext context)
    {
        return _serviceProvidersPerSession.GetOrAdd(
            context.Request.Session.SessionUid.Value,
            _ => new TUnitServiceProvider(
                _extension,
                context,
                context.MessageBus,
                _frameworkServiceProvider,
                _capabilities));
    }

    private static bool IsCancellationException(Exception e)
    {
        return e is TaskCanceledException or OperationCanceledException;
    }

    private async Task ReportUnhandledException(ExecuteRequestContext context, Exception exception)
    {
        await context.MessageBus.PublishAsync(
            dataProducer: this,
            data: new TestNodeUpdateMessage(
                sessionUid: context.Request.Session.SessionUid,
                testNode: new TestNode
                {
                    DisplayName = $"Unhandled exception - {exception.GetType().Name}: {exception.Message}",
                    Uid = new TestNodeUid(Guid.NewGuid().ToString()),
                    Properties = new PropertyBag(new ErrorTestNodeStateProperty(exception))
                }));
    }
}

/// <summary>
/// Handles different types of test execution requests
/// </summary>
internal interface IRequestHandler
{
    Task HandleRequestAsync(TestExecutionRequest request, TUnitServiceProvider serviceProvider, ExecuteRequestContext context);
}

/// <summary>
/// Default implementation of request handler
/// </summary>
internal sealed class TestRequestHandler : IRequestHandler
{
    public async Task HandleRequestAsync(TestExecutionRequest request, TUnitServiceProvider serviceProvider, ExecuteRequestContext context)
    {
        switch (request)
        {
            case DiscoverTestExecutionRequest discoverRequest:
                await HandleDiscoveryRequestAsync(serviceProvider, discoverRequest, context);
                break;

            case RunTestExecutionRequest runRequest:
                await HandleRunRequestAsync(serviceProvider, runRequest, context);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.GetType().Name,
                    "Unknown request type");
        }
    }

    private async Task HandleDiscoveryRequestAsync(
        TUnitServiceProvider serviceProvider,
        DiscoverTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        var allTests = await serviceProvider.DiscoveryService.DiscoverTests();

        foreach (var test in allTests)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await serviceProvider.MessageBus.Discovered(test.Context!);
        }
    }

    private async Task HandleRunRequestAsync(
        TUnitServiceProvider serviceProvider,
        RunTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        var allTests = await serviceProvider.DiscoveryService.DiscoverTests();

        // Apply filter to tests before reporting discovery
        var testsToRun = allTests;
        if (request.Filter != null)
        {
            // Create a null logger factory for now - filtering will still work
            var loggerFactory = new NullLoggerFactory();
            var filterService = new TestFilterService(loggerFactory);
            testsToRun = filterService.FilterTests(request, allTests.ToArray()).ToList();
            
            // Debug logging
            System.IO.File.AppendAllText("/tmp/tunit-framework-debug.log", 
                $"TUnitTestFramework: Filtered {testsToRun.Count()} tests from {allTests.Count()} total\n");
        }

        // Report only the tests that will actually run
        foreach (var test in testsToRun)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            await serviceProvider.MessageBus.Discovered(test.Context!);
        }

        // Execute tests (executor will apply the same filter internally)
        await serviceProvider.TestExecutor.ExecuteTests(
            allTests,
            request.Filter,
            context.MessageBus,
            context.CancellationToken);
    }
}
