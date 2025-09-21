using TUnit.Assertions.Enums;

namespace TUnit.Assertions;

public record CompareOptions
{
    public string[] MembersToIgnore { get; init; } = [];
    public Type[] TypesToIgnore { get; init; } = [];
    public EquivalencyKind EquivalencyKind { get; set; } = EquivalencyKind.Full;
}
