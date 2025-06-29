using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Key-value pair string property
/// </summary>
public class KeyValuePairStringProperty : IProperty
{
    public string Key { get; }
    public string Value { get; }
    
    public KeyValuePairStringProperty(string key, string value)
    {
        Key = key;
        Value = value;
    }
}