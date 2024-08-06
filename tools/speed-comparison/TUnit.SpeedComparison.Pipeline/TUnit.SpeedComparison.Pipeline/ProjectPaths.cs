using File = ModularPipelines.FileSystem.File;

namespace TUnit.SpeedComparison.Pipeline;

public record ProjectPaths
{
    public required File TUnit { get; init; }
    public required File xUnit { get; init; }
    public required File NUnit { get; init; }
    public required File MSTest { get; init; }
}