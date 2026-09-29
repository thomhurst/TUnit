namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// All hooks declared by one class, emitted together into a single generated file.
/// Value equality over the hook models means only the file for a class whose hooks changed is re-emitted.
/// </summary>
public sealed record HookClassGroup
{
    public required string FullyQualifiedTypeName { get; init; }
    public required EquatableArray<HookModel> Hooks { get; init; }
}
