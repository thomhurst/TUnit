using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

internal class MethodParameterTypesProperty : IProperty
{
    public IReadOnlyList<string>? FullyQualifiedTypeNames { get; }

    public MethodParameterTypesProperty(IReadOnlyList<string>? fullyQualifiedTypeNames)
    {
        FullyQualifiedTypeNames = fullyQualifiedTypeNames;
    }
}