using Microsoft.CodeAnalysis.Testing;

namespace TUnit.Assertions.SourceGenerator.Tests.Options;

public record RunTestOptions
{
    public string[] AdditionalFiles { get; set; } = [];
    public Dictionary<string, string>? BuildProperties { get; set; }
    public string[] AdditionalSyntaxes { get; set; } = [];
    public PackageIdentity[] AdditionalPackages { get; set; } = [];
}
