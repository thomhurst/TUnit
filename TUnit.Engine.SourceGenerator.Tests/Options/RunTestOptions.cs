namespace TUnit.Engine.SourceGenerator.Tests.Options;

public record RunTestOptions
{
    public string[] AdditionalFiles { get; set; } = [];
}