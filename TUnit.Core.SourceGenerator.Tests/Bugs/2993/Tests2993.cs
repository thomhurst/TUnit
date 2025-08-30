namespace TUnit.Core.SourceGenerator.Tests.Bugs._2993;

internal class Tests2993 : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "2993",
            "CompilationFailureTests.cs"),
        async generatedFiles =>
        {
            // Scrub GUIDs from generated files before verification (same as other tests)
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