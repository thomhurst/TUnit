using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;

namespace TUnit.Core;

public record DataGeneratorMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public Type TestClassType => TestInformation.Class.Type;
    public required TestBuilderContextAccessor TestBuilderContext { get; init; }
    public required MemberMetadata[] MembersToGenerate { get; init; }
    public required MethodMetadata TestInformation { get; init; }
    public required DataGeneratorType Type { get; init; }
    public required string TestSessionId { get; init; }
    public required object? TestClassInstance { get; init; }
    public required object?[]? ClassInstanceArguments { get; init; }
}
