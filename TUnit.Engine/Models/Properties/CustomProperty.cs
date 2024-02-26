using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

public class CustomProperty : IProperty
{
    public string Key { get; }
    public string Value { get; }

    public CustomProperty(string key, string value)
    {
        Key = key;
        Value = value;
    }
}