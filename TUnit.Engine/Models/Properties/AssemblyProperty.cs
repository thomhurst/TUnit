using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

public class AssemblyProperty : IProperty
{
    public string FullyQualifiedAssembly { get; }

    public AssemblyProperty(string fullyQualifiedAssembly)
    {
        FullyQualifiedAssembly = fullyQualifiedAssembly;
    }
}