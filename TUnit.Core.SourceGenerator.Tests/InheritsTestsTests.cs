using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritsTestsTests : TestsBase
{
    [Test]
    public Task Test() => TestMetadataGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "1924",
            "None",
            "Tests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "Bugs", "1924", "DataClass.cs")
            ]
        },
        async generatedFiles =>
        {
            // BaseClass has 1 test method with [Repeat(10)] and 3 [Arguments]
            // So BaseClass should generate 1 file (per-class)
            // Tests, Tests2, Tests3 each inherit this test, so 3 more files
            // With per-class consolidation, class names no longer contain method names
            // Verify that each inheriting class has a generated test file
            var hasTests1 = generatedFiles.Any(f => f.Contains("class") && f.Contains("Tests_Inherited_TestSource") && !f.Contains("Tests2") && !f.Contains("Tests3"));
            var hasTests2 = generatedFiles.Any(f => f.Contains("Tests2_Inherited_TestSource"));
            var hasTests3 = generatedFiles.Any(f => f.Contains("Tests3_Inherited_TestSource"));
            var hasBaseClass = generatedFiles.Any(f => f.Contains("BaseClass_TestSource"));

            await Assert.That(hasBaseClass).IsTrue();
            await Assert.That(hasTests1).IsTrue();
            await Assert.That(hasTests2).IsTrue();
            await Assert.That(hasTests3).IsTrue();
        });
}