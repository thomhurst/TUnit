using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Engine.Extensions;
using TUnit.TestAdapter;

namespace TUnit.Engine;

internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly Func<IEnumerable<Assembly>> _getTestAssemblies;
    private readonly ITestFrameworkCapabilities _capabilities;
    private readonly ServiceProvider _myServiceProvider;

    public TUnitTestFramework(IExtension extension,
        Func<IEnumerable<Assembly>> getTestAssemblies,
        IServiceProvider serviceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        _extension = extension;
        _getTestAssemblies = getTestAssemblies;
        _capabilities = capabilities;
        
        _myServiceProvider = new ServiceCollection()
            .AddTestEngineServices()
            .AddFromFrameworkServiceProvider(serviceProvider)
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
                        foreach (var testNode in _myServiceProvider.GetRequiredService<TUnitTestDiscoverer>()
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