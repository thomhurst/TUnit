using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using TUnit.Engine.Extensions;

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
            .AddFromFrameworkServiceProvider(serviceProvider, extension)
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
                var testNodes = _myServiceProvider.GetRequiredService<TUnitTestDiscoverer>()
                    .DiscoverTests(context.Request as TestExecutionRequest, _getTestAssemblies, context.CancellationToken)
                    .ToList();

                if (context.Request is DiscoverTestExecutionRequest)
                {
                    foreach (var testNode in testNodes)
                    {
                        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                            sessionUid: context.Request.Session.SessionUid,
                            testNode: testNode)
                        );
                    }
                }
                else if (context.Request is RunTestExecutionRequest)
                {
                    await _myServiceProvider.GetRequiredService<TestsExecutor>()
                        .ExecuteAsync(testNodes, context.Request.Session);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(context.Request), context.Request.GetType().Name);
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