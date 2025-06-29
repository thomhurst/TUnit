using Microsoft.Testing.Platform.Configurations;

namespace TUnit.Engine.Framework;

internal class ConfigurationAdapter(IConfiguration configuration) : Core.Interfaces.IConfiguration
{
    public string? Get(string key)
    {
        return configuration[key];
    }
}
