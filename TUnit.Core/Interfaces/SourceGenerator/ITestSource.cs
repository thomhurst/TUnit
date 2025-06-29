namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSource
{
    Task<DiscoveryResult> DiscoverTestsAsync(string sessionId);
}
