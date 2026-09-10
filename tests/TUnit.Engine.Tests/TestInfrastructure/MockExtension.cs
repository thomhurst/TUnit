using Microsoft.Testing.Platform.Extensions;

namespace TUnit.Engine.Tests;

internal sealed class MockExtension : IExtension
{
    public string Uid => "MockExtension";
    public string DisplayName => "Mock";
    public string Version => "1.0.0";
    public string Description => "Mock Extension";
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
