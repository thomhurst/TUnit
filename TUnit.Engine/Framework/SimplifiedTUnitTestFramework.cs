using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Services;
using TUnit.Core;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Framework;

/// <summary>
/// Simplified test framework implementation using the new architecture
/// </summary>
internal sealed class SimplifiedTUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly IServiceProvider _frameworkServiceProvider;
    private readonly ITestFrameworkCapabilities _capabilities;
    private static readonly ConcurrentDictionary<string, SimplifiedTUnitServiceProvider> ServiceProvidersPerSession = [];

    public SimplifiedTUnitTestFramework(
        IExtension extension,
        IServiceProvider frameworkServiceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        _extension = extension;
        _frameworkServiceProvider = frameworkServiceProvider;
        _capabilities = capabilities;
    }

    public Task<bool> IsEnabledAsync() => _extension.IsEnabledAsync();

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
    }

    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        var serviceProvider = ServiceProvidersPerSession.GetOrAdd(
            context.Request.Session.SessionUid.Value,
            _ => new SimplifiedTUnitServiceProvider(
                _extension, 
                context, 
                context.MessageBus, 
                _frameworkServiceProvider, 
                _capabilities));

        try
        {
            serviceProvider.CancellationToken.Initialise(context.CancellationToken);
            
            switch (context.Request)
            {
                case DiscoverTestExecutionRequest discoverRequest:
                    await HandleDiscoveryRequest(serviceProvider, discoverRequest, context);
                    break;
                    
                case RunTestExecutionRequest runRequest:
                    await HandleRunRequest(serviceProvider, runRequest, context);
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(context.Request), 
                        context.Request.GetType().Name,
                        "Unknown request type");
            }
        }
        catch (Exception e) when (e is TaskCanceledException or OperationCanceledException && 
                                 context.CancellationToken.IsCancellationRequested)
        {
            await serviceProvider.Logger.LogErrorAsync("The test run was cancelled.");
        }
        catch (Exception e)
        {
            await serviceProvider.Logger.LogErrorAsync(e);
            await ReportUnhandledException(context, e);
        }
    }

    private async Task HandleDiscoveryRequest(
        SimplifiedTUnitServiceProvider serviceProvider,
        DiscoverTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        // Discover all tests
        var allTests = await serviceProvider.DiscoveryService.DiscoverTests();
        
        // Report discovered tests
        foreach (var test in allTests)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;
                
            await serviceProvider.MessageBus.Discovered(test.Context!);
        }
    }

    private async Task HandleRunRequest(
        SimplifiedTUnitServiceProvider serviceProvider,
        RunTestExecutionRequest request,
        ExecuteRequestContext context)
    {
        // Discover all tests
        var allTests = await serviceProvider.DiscoveryService.DiscoverTests();
        
        // Execute tests
        await serviceProvider.TestExecutor.ExecuteTests(
            allTests,
            request.Filter,
            context.MessageBus,
            context.CancellationToken);
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

    public async Task CloseTestSessionAsync(CloseTestSessionContext context)
    {
        if (ServiceProvidersPerSession.TryRemove(context.SessionUid.Value, out var serviceProvider))
        {
            await serviceProvider.DisposeAsync();
        }
    }
}