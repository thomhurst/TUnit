namespace TUnit.Core;

public record ClassConstructorMetadata
{
    public required string TestSessionId { get; init; }
    public required TestBuilderContext TestBuilderContext { get; init; }
}
