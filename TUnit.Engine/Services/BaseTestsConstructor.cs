using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Services;

internal abstract class BaseTestsConstructor(IExtension extension,
    DependencyCollector dependencyCollector) : IDataProducer
{
    public async Task<DiscoveredTest[]> GetTestsAsync(CancellationToken cancellationToken)
    {
        var discoveredTests = await DiscoverTestsAsync();

        dependencyCollector.ResolveDependencies(discoveredTests, cancellationToken);

        return discoveredTests;
    }

    protected abstract Task<DiscoveredTest[]> DiscoverTestsAsync();

    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;
    public string Version => extension.Version;
    public string DisplayName => extension.DisplayName;
    public string Description => extension.Description;
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];
}
