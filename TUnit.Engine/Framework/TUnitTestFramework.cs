using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;

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
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => _extension.IsEnabledAsync();

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
    }

    [UnconditionalSuppressMessage("Trimming", "IL3051:RequiresDynamicCodeAttribute",
        Justification = "The interface doesn't have the attribute, but we handle AOT-safe paths internally")]
    [UnconditionalSuppressMessage("Trimming", "IL2046:RequiresUnreferencedCodeAttribute",
        Justification = "The interface doesn't have the attribute, but we handle AOT-safe paths internally")]
    [RequiresDynamicCode("Generic type resolution requires runtime type generation.")]
    [RequiresUnreferencedCode("Generic type resolution may access types not preserved by trimming.")]
    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        var serviceProvider = GetOrCreateServiceProvider(context);

        try
        {
            serviceProvider.CancellationToken.Initialise(context.CancellationToken);
            await _requestHandler.HandleRequestAsync((TestExecutionRequest) context.Request, serviceProvider, context);
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
