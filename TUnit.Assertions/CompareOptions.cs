namespace TUnit.Assertions;

public record CompareOptions
{
    public string[] MembersToIgnore { get; init; } = [];
}