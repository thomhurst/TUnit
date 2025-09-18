using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ErrorSymbolTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "ErrorSymbolTestsTestFile.cs"),
        new RunTestOptions
        {
            // Allow compilation errors for this test
            AllowCompilationErrors = true
        },
        async generatedFiles =>
        {
            // Should generate no files because the test classes have error symbols
            await Assert.That(generatedFiles).HasCount().EqualTo(0);
        });
}