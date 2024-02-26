using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

public class ClassInformationProperty : IProperty
{
    public string SimpleName { get; }
    public string FullyQualifiedName { get; }
    public string AssemblyQualifiedName { get; }

    public ClassInformationProperty(string simpleName, string fullyQualifiedName, string assemblyQualifiedName)
    {
        SimpleName = simpleName;
        FullyQualifiedName = fullyQualifiedName;
        AssemblyQualifiedName = assemblyQualifiedName;
    }
}