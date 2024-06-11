using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;

namespace TUnit.Engine;

internal class ConsoleWriter : IOutputDeviceDataProducer
{
    private readonly IExtension _extension;
    private readonly IOutputDevice _outputDevice;

    public ConsoleWriter(IExtension extension, IOutputDevice outputDevice)
    {
        _extension = extension;
        _outputDevice = outputDevice;
    }

    public async Task Write(string text)
    {
        await _outputDevice.DisplayAsync(this, new TextOutputDeviceData(text));
    }

    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(true);
    }

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;
}