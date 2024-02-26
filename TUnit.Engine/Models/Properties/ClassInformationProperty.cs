using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

internal class ClassInformationProperty : IProperty
{
    public required string SimpleName { get; init; }
    public required string FullyQualifiedName { get; init; }
    public required string AssemblyQualifiedName { get; init; }
}