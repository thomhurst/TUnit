using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritsTestsEmptyDataSourceTests : TestsBase
{
    [Test]
    public Task Test() => TestMetadataGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.Core.SourceGenerator.Tests",
            "Bugs",
            "2678",
            "ConcreteTestWithEmptyDataSources.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName, "TUnit.Core.SourceGenerator.Tests", "Bugs", "2678", "AbstractTestWithEmptyDataSources.cs")
            ]
        },
        async generatedFiles =>
        {
            // The test should generate successfully without throwing "Sequence contains no matching element"
            // We expect files to be generated for the inherited tests even with empty data sources
            var testFiles = generatedFiles.Where(f => f.Contains("_Test_") || f.Contains("ConcreteTestWithEmptyDataSources")).ToArray();
            
            // Verify that tests are generated even with empty data sources
            // The key is that this should not throw an exception during generation
            await Assert.That(testFiles.Length).IsGreaterThan(0);
            
            // Check that inherited tests are generated
            var hasInheritedTests = testFiles.Any(f => f.Contains("ConcreteTestWithEmptyDataSources_"));
            await Assert.That(hasInheritedTests).IsTrue();
        });
}