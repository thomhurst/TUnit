using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;

namespace TUnit.Engine.Framework;

/// <summary>
/// Unified test framework implementation with proper ExecutionContext handling
/// </summary>
internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly IServiceProvider _frameworkServiceProvider;
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly ConcurrentDictionary<string, SimplifiedTUnitServiceProvider> _serviceProvidersPerSession = new();
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

    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        var serviceProvider = GetOrCreateServiceProvider(context);

        try
        {
            serviceProvider.CancellationToken.Initialise(context.CancellationToken);
            await _requestHandler.HandleRequestAsync((TestExecutionRequest)context.Request, serviceProvider, context);
        }
        catch (Exception e) when (IsCancellationException(e) && context.CancellationToken.IsCancellationRequested)
        {
            await serviceProvider.Logger.LogErrorAsync("The test run was cancelled.");
        }
        catch (Exception e)
        {
            await serviceProvider.Logger.LogErrorAsync(e);
            await ReportUnhandledException(context, e);
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

    private SimplifiedTUnitServiceProvider GetOrCreateServiceProvider(ExecuteRequestContext context)
    {
        return _serviceProvidersPerSession.GetOrAdd(
            context.Request.Session.SessionUid.Value,
            _ => new SimplifiedTUnitServiceProvider(
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
    Task HandleRequestAsync(TestExecutionRequest request, SimplifiedTUnitServiceProvider serviceProvider, ExecuteRequestContext context);
}

/// <summary>
/// Default implementation of request handler
/// </summary>
internal sealed class TestRequestHandler : IRequestHandler
{
    public async Task HandleRequestAsync(TestExecutionRequest request, SimplifiedTUnitServiceProvider serviceProvider, ExecuteRequestContext context)
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
        SimplifiedTUnitServiceProvider serviceProvider,
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
        SimplifiedTUnitServiceProvider serviceProvider,
        RunTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        var allTests = await serviceProvider.DiscoveryService.DiscoverTests();
        
        // Report discovered tests during run (some runners need this)
        foreach (var test in allTests)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;
                
            await serviceProvider.MessageBus.Discovered(test.Context!);
        }
        
        // Execute tests
        await serviceProvider.TestExecutor.ExecuteTests(
            allTests,
            request.Filter,
            context.MessageBus,
            context.CancellationToken);
    }
}