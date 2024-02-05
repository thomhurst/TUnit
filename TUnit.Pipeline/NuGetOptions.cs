using ModularPipelines.Attributes;

namespace TUnit.Pipeline;

public record NuGetOptions
{
    [SecretValue]
    public string? ApiKey { get; set; }
}