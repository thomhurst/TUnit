namespace TUnit.Core;

public class Artifact
{
    public required FileInfo File { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
}
