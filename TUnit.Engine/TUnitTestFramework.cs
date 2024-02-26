using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Extensions.VSTestBridge;
using Microsoft.Testing.Extensions.VSTestBridge.Requests;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Engine.Extensions;

namespace TUnit.TestAdapter;

internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly Func<IEnumerable<Assembly>> _getTestAssemblies;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly ServiceProvider _myServiceProvider;

    public TUnitTestFramework(IExtension extension,
        Func<IEnumerable<Assembly>> getTestAssemblies,
        IServiceProvider serviceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        _extension = extension;
        _getTestAssemblies = getTestAssemblies;
        _serviceProvider = serviceProvider;
        _capabilities = capabilities;

        _myServiceProvider = new ServiceCollection()
            .AddTestEngineServices()
            .AddFromFrameworkServiceProvider()
            .BuildServiceProvider();
    }

    public Task<bool> IsEnabledAsync() => _extension.IsEnabledAsync();

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;
    
    public async Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        await Task.CompletedTask;

        return new CreateTestSessionResult
        {
            IsSuccess = true
        };
    }

    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        await using (_myServiceProvider)
        {
            try
            {
                switch (context.Request)
                {
                    case DiscoverTestExecutionRequest discoverTestExecutionRequest:
                        foreach (var testNode in _serviceProvider.GetRequiredService<TUnitTestDiscoverer>()
                                     .DiscoverTests(discoverTestExecutionRequest, _getTestAssemblies, context.CancellationToken))
                        {
                            await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                                sessionUid: context.Request.Session.SessionUid,
                                testNode: testNode)
                            );
                        }
                        break;
                    case RunTestExecutionRequest runTestExecutionRequest:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                context.Complete();
            }
        }
    }
    
    public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        return Task.FromResult(new CloseTestSessionResult
        {
            IsSuccess = true
        });
    }

    public Type[] DataTypesProduced { get; } = [typeof(TestNodeUpdateMessage)];
}