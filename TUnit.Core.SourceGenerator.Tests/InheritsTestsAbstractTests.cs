using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritsTestsAbstractTests : TestsBase
{
    [Test]
    public Task Test() => TestMetadataGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AbstractTests",
            "ConcreteClass2.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "AbstractTests", "AbstractBaseClass.cs"),
                Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "AbstractTests", "ConcreteClass1.cs")
            ]
        },
        async generatedFiles =>
        {
            // Check multiple files since we have AbstractBaseClass.cs, ConcreteClass1.cs, ConcreteClass2.cs
            // AbstractBaseClass has 1 test method
            // ConcreteClass1 has [InheritsTests] - should generate inherited test
            // ConcreteClass2 extends ConcreteClass1 and has [InheritsTests] + its own test - should generate inherited test + own test

            // With per-class consolidation, method names appear inside generated classes, not in class names
            // Verify ConcreteClass1 has inherited test (method name appears inside the generated file)
            var hasConcreteClass1InheritedTest = generatedFiles.Any(f => f.Contains("ConcreteClass1_Inherited_TestSource") && f.Contains("AssertClassName"));
            await Assert.That(hasConcreteClass1InheritedTest).IsTrue();

            // Verify ConcreteClass2 has both inherited test and its own test
            var hasConcreteClass2InheritedTest = generatedFiles.Any(f => f.Contains("ConcreteClass2_Inherited_TestSource") && f.Contains("AssertClassName"));
            var hasConcreteClass2OwnTest = generatedFiles.Any(f => f.Contains("ConcreteClass2_TestSource") && f.Contains("SecondTest"));

            await Assert.That(hasConcreteClass2InheritedTest).IsTrue();
            await Assert.That(hasConcreteClass2OwnTest).IsTrue();
        });
}