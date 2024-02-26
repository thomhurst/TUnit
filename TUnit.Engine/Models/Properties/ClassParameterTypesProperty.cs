using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

internal class ClassParameterTypesProperty(IReadOnlyList<string>? fullyQualifiedTypeNames) : IProperty
{
    public IReadOnlyList<string>? FullyQualifiedTypeNames { get; } = fullyQualifiedTypeNames;
}