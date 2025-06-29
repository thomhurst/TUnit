using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Standard output property
/// </summary>
public class StandardOutputProperty : IProperty
{
    public string Output { get; }
    
    public StandardOutputProperty(string output)
    {
        Output = output;
    }
}