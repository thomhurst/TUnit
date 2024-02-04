using ModularPipelines.Attributes;

public record NuGetOptions
{
    [SecretValue]
    public string? ApiKey { get; set; }
}