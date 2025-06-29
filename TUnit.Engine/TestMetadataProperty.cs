using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Test metadata property
/// </summary>
public class TestMetadataProperty : IProperty
{
    public string Value { get; }
    
    public TestMetadataProperty(string value)
    {
        Value = value;
    }
}