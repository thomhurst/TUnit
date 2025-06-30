namespace TUnit.Engine;

/// <summary>
/// Represents a file artifact
/// </summary>
public class FileArtifact
{
    public required string File { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
}
