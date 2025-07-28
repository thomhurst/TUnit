namespace TUnit.Core.SourceGenerator.Tests;

internal class InheritsTestsAbstractTests : TestsBase
{
    [Test]
    public Task Test() => TestMetadataGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AbstractTests"),
        async generatedFiles =>
        {
            // Check multiple files since we have AbstractBaseClass.cs, ConcreteClass1.cs, ConcreteClass2.cs
            // AbstractBaseClass has 1 test method
            // ConcreteClass1 has [InheritsTests] - should generate inherited test
            // ConcreteClass2 extends ConcreteClass1 and has [InheritsTests] + its own test - should generate inherited test + own test
            
            // Filter to only test-related generated files
            var testFiles = generatedFiles.Where(f => f.Contains("_Test_") || f.Contains("ConcreteClass")).ToArray();
            
            // Verify ConcreteClass1 has inherited test
            var hasConcreteClass1InheritedTest = testFiles.Any(f => f.Contains("ConcreteClass1_AssertClassName"));
            await Assert.That(hasConcreteClass1InheritedTest).IsTrue();
            
            // Verify ConcreteClass2 has both inherited test and its own test
            var hasConcreteClass2InheritedTest = testFiles.Any(f => f.Contains("ConcreteClass2_AssertClassName"));
            var hasConcreteClass2OwnTest = testFiles.Any(f => f.Contains("ConcreteClass2_SecondTest"));
            
            await Assert.That(hasConcreteClass2InheritedTest).IsTrue();
            await Assert.That(hasConcreteClass2OwnTest).IsTrue();
        });
}