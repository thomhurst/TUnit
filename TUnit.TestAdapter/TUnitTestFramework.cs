using System.Reflection;
using Microsoft.Testing.Extensions.VSTestBridge;
using Microsoft.Testing.Extensions.VSTestBridge.Requests;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TUnit.TestAdapter;

internal sealed class TUnitTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;
    private readonly Func<IEnumerable<Assembly>> _getTestAssemblies;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITestFrameworkCapabilities _capabilities;

    public TUnitTestFramework(IExtension extension,
        Func<IEnumerable<Assembly>> getTestAssemblies,
        IServiceProvider serviceProvider,
        ITestFrameworkCapabilities capabilities)
    {
        _extension = extension;
        _getTestAssemblies = getTestAssemblies;
        _serviceProvider = serviceProvider;
        _capabilities = capabilities;
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
        switch (context.Request)
        {
            case VSTestRunTestExecutionRequest vsTestRunTestExecutionRequest:
                break;
            case VSTestDiscoverTestExecutionRequest vsTestDiscoverTestExecutionRequest:
                break;
            case DiscoverTestExecutionRequest discoverTestExecutionRequest:
                switch (discoverTestExecutionRequest.Filter)
                {
                    case VSTestTestExecutionFilter vsTestTestExecutionFilter:
                        break;
                    case TestNodeUidListFilter testNodeUidListFilter:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            case RunTestExecutionRequest runTestExecutionRequest:
                switch (runTestExecutionRequest.Session)
                {
                    case CloseTestSessionContext closeTestSessionContext:
                        break;
                    case CreateTestSessionContext createTestSessionContext:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            case TestExecutionRequest testExecutionRequest:
                switch (testExecutionRequest.Session)
                {
                    
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        await Task.CompletedTask;
    }
    
    public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        await Task.CompletedTask;
        
        return new CloseTestSessionResult
        {
            IsSuccess = true
        };
    }

    public Type[] DataTypesProduced { get; } = Array.Empty<Type>();
}