using TUnit.Core.Enums;

namespace TUnit.Core;

public record DataGeneratorMetadata
{
    public required TestBuilderContextAccessor TestBuilderContext { get; init; }
    public required IMemberMetadata[] MembersToGenerate { get; init; }
    public required MethodMetadata? TestInformation { get; init; }
    public required DataGeneratorType Type { get; init; }
    public required string TestSessionId { get; init; }
    public required object? TestClassInstance { get; init; }
    public required object?[]? ClassInstanceArguments { get; init; }

    /// <summary>
    /// Optional factory for creating and initializing test class instances.
    /// Used in reflection mode when instance data sources depend on property injection.
    /// The factory should create an instance and perform property injection before returning it.
    /// </summary>
    public Func<Type, Task<object?>>? InstanceFactory { get; init; }
}
