using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class Bugs2971NullableTypeTest : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "2971",
            "NullableTypeTest.cs"),
        new RunTestOptions
        {
            AdditionalFiles = [Path.Combine(Git.RootDirectory.FullName,
                "TUnit.TestProject",
                "Bugs",
                "2971",
                "AssemblyInfo.cs")],
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            // Scrub GUIDs from generated files before verification (same as TestsBase)
            var scrubbedFiles = generatedFiles.Select(file => ScrubGuids(file)).ToArray();
            await Verify(scrubbedFiles).UniqueForTargetFrameworkAndVersion();
        });

    private string ScrubGuids(string text)
    {
        var result = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\\r\\n", "\\n")
            .Replace("\\r", "\\n");

        // Scrub GUIDs from class names and identifiers
        // Pattern 1: TestSource classes - ClassName_MethodName_TestSource_[32 hex chars]
        var guidPattern1 = @"_TestSource_[a-fA-F0-9]{32}";
        var scrubbedText = System.Text.RegularExpressions.Regex.Replace(result, guidPattern1, "_TestSource_GUID", System.Text.RegularExpressions.RegexOptions.None);

        // Pattern 2: ModuleInitializer classes - ClassName_MethodName_ModuleInitializer_[32 hex chars]
        var guidPattern2 = @"_ModuleInitializer_[a-fA-F0-9]{32}";
        scrubbedText = System.Text.RegularExpressions.Regex.Replace(scrubbedText, guidPattern2, "_ModuleInitializer_GUID", System.Text.RegularExpressions.RegexOptions.None);

        return scrubbedText;
    }
}