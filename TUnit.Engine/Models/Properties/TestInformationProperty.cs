using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

internal class TestInformationProperty : IProperty
{
    public required string UniqueId { get; init; }
    public required string TestName { get; init; }
    public required bool IsSingleTest { get; init; }
    public required bool IsStatic { get; init; }
}