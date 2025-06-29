using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Standard error property
/// </summary>
public class StandardErrorProperty : IProperty
{
    public string Error { get; }
    
    public StandardErrorProperty(string error)
    {
        Error = error;
    }
}