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
        async generatedFiles =>
        {
            // BaseClass has 1 test method with [Repeat(10)] and 3 [Arguments]
            // So BaseClass should generate 1 file
            // Tests, Tests2, Tests3 each inherit this test, so 3 more files
            // Total: 4 generated files (1 for base, 3 for inheriting classes)
            // Verify that each inheriting class has a generated test file
            var hasTests1 = generatedFiles.Any(f => f.Contains("Tests_Test_") && !f.Contains("Tests2") && !f.Contains("Tests3"));
            var hasTests2 = generatedFiles.Any(f => f.Contains("Tests2_Test_"));
            var hasTests3 = generatedFiles.Any(f => f.Contains("Tests3_Test_"));
            var hasBaseClass = generatedFiles.Any(f => f.Contains("BaseClass_Test_"));
            
            await Assert.That(hasBaseClass).IsTrue();
            await Assert.That(hasTests1).IsTrue();
            await Assert.That(hasTests2).IsTrue();
            await Assert.That(hasTests3).IsTrue();
        });
}