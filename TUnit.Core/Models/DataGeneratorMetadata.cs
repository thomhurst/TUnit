using TUnit.Core.Enums;

namespace TUnit.Core;

public record DataGeneratorMetadata
{
    public Type TestClassType => TestInformation.Class.Type;
    public required TestBuilderContextAccessor TestBuilderContext { get; init; }
    public required SourceGeneratedMemberInformation[] MembersToGenerate { get; init; }
    public required SourceGeneratedMethodInformation TestInformation { get; init; }
    public required DataGeneratorType Type { get; init; }
    public required string TestSessionId { get; init; }
    public required object? TestClassInstance { get; init; }
    public required object?[]? ClassInstanceArguments { get; init; }
}