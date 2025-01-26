using TUnit.Core.Enums;

namespace TUnit.Core;

public record DataGeneratorMetadata
{
    public required Type TestClassType { get; init; }
    public required TestBuilderContextAccessor TestBuilderContext { get; init; }
    public required SourceGeneratedMemberInformation[] MembersToGenerate { get; init; }
    public required SourceGeneratedTestInformation TestInformation { get; init; }
    public required DataGeneratorType Type { get; init; }
    public required string TestSessionId { get; init; }
}