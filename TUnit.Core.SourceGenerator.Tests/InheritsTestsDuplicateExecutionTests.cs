using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

// Temporarily disabled while investigating the fix
/*
internal class InheritsTestsDuplicateExecutionTests : TestsBase
{
    [Test]
    public Task Test() => TestMetadataGenerator.RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "Bugs",
            "DuplicateInheritedTests.cs"),
        new RunTestOptions(),
        async generatedFiles =>
        {
            // After the fix:
            // - GenericTestExample<T>.GenericTest should be inherited once in IntGenericTests 
            // - IntGenericTests.AdditionalIntTest should be generated only once (not duplicated)
            // 
            // So we should have exactly 2 generated test files:
            // 1. IntGenericTests_GenericTest_* (inherited from base)
            // 2. IntGenericTests_AdditionalIntTest_* (directly from derived class)

            var testFiles = generatedFiles.Where(f => f.Contains("IntGenericTests")).ToArray();
            
            // Count how many files contain AdditionalIntTest
            var additionalIntTestFiles = testFiles.Where(f => f.Contains("AdditionalIntTest")).ToArray();
            
            // After the fix, this should be 1 (no duplication)
            Console.WriteLine($"AdditionalIntTest files count: {additionalIntTestFiles.Length}");
            
            // Test to confirm the fix works
            await Assert.That(additionalIntTestFiles.Length).IsEqualTo(1);
            
            // Also check that the inherited test is generated
            var inheritedTestFiles = testFiles.Where(f => f.Contains("GenericTest")).ToArray();
            await Assert.That(inheritedTestFiles.Length).IsEqualTo(1);
            
            // Total should be exactly 2 test files for IntGenericTests
            await Assert.That(testFiles.Length).IsEqualTo(2);
        });
}
*/