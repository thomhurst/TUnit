using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Engine.Diagnostics;

namespace TUnit.Engine.Framework;

/// Unified test framework with ExecutionContext handling and global exception management
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

        ConfigureDebugListeners();
    }

    private static void ConfigureDebugListeners()
    {
        // Remove default listeners that can show UI dialogs
        Trace.Listeners.Clear();

        // Add our custom listener that converts assertions to exceptions
        var assertionListener = new TUnitAssertionListener();
        Trace.Listeners.Add(assertionListener);

        // Configure to not show assert UI dialogs
        Trace.AutoFlush = true;
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

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => _extension.IsEnabledAsync();

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
    }

    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        try
        {
            ConfigureGlobalExceptionHandlers(context);

            var serviceProvider = GetOrCreateServiceProvider(context);

            GlobalContext.Current = serviceProvider.ContextProvider.GlobalContext;
            BeforeTestDiscoveryContext.Current = serviceProvider.ContextProvider.BeforeTestDiscoveryContext;
            TestDiscoveryContext.Current = serviceProvider.ContextProvider.TestDiscoveryContext;
            TestSessionContext.Current = serviceProvider.ContextProvider.TestSessionContext;

            serviceProvider.CancellationToken.Initialise(context.CancellationToken);

            await _requestHandler.HandleRequestAsync((TestExecutionRequest) context.Request, serviceProvider, context, GetFilter(context));
        }
        catch (Exception e) when (IsCancellationException(e))
        {
            // Check if this is a normal cancellation or fail-fast cancellation
            if (context.CancellationToken.IsCancellationRequested)
            {
                await GetOrCreateServiceProvider(context).Logger.LogErrorAsync("The test run was cancelled.");
            }
            else
            {
                // This is likely a fail-fast cancellation
                await GetOrCreateServiceProvider(context).Logger.LogErrorAsync("Test execution stopped due to fail-fast.");
            }

            throw;
        }
        catch (Exception e)
        {
            await GetOrCreateServiceProvider(context).Logger.LogErrorAsync(e);
            await ReportUnhandledException(context, e);
            throw;
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

    private TUnitServiceProvider GetOrCreateServiceProvider(ExecuteRequestContext context)
    {
        return _serviceProvidersPerSession.GetOrAdd(
            context.Request.Session.SessionUid.Value,
            _ => new TUnitServiceProvider(
                _extension,
                context,
                GetFilter(context),
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

    private ITestExecutionFilter? GetFilter(ExecuteRequestContext context)
    {
        if (context.Request is RunTestExecutionRequest runRequest)
        {
            return runRequest.Filter;
        }

        if (context.Request is DiscoverTestExecutionRequest discoverTestExecutionRequest)
        {
            return discoverTestExecutionRequest.Filter;
        }

        return null;
    }
}
