using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritedTestsFromDifferentProjectTests : TestsBase
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "InheritedTestsFromDifferentProjectTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject.Library",
                    "BaseTests.cs"),
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "TestData.cs"),
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "Attributes",
                    "ExpectedPassEngineTest.cs")
            ]
        },
        async generatedFiles =>
        {
            // Verify that inherited test methods have their categories properly included
            var generatedCode = string.Join(Environment.NewLine, generatedFiles);
            
            // Check that the BaseTest method has the BaseCategory attribute
            await Assert.That(generatedCode).Contains("new global::TUnit.Core.CategoryAttribute(\"BaseCategory\")");
            
            // Check that the BaseTestWithMultipleCategories method has both category attributes
            await Assert.That(generatedCode).Contains("new global::TUnit.Core.CategoryAttribute(\"AnotherBaseCategory\")");
            await Assert.That(generatedCode).Contains("new global::TUnit.Core.CategoryAttribute(\"MultipleCategories\")");
        });
}
