namespace TUnit.Core.SourceGenerator.Tests.Options;

public record RunTestOptions
{
    public string[] AdditionalFiles { get; set; } = [];
    public Dictionary<string, string>? BuildProperties { get; set; }
    public Func<SettingsTask, SettingsTask>? VerifyConfigurator { get; set; }
}