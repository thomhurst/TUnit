using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

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
            // Debug: print what's actually being generated
            var allTestFiles = generatedFiles.ToArray();
            Console.WriteLine($"Total generated files: {allTestFiles.Length}");
            
            foreach (var file in allTestFiles)
            {
                var classNameLine = file.Split('\n').FirstOrDefault(line => line.Contains("internal sealed class"));
                if (classNameLine != null)
                {
                    Console.WriteLine($"Generated class: {classNameLine.Trim()}");
                }
            }
            
            var intGenericTestFiles = allTestFiles.Where(f => f.Contains("IntGenericTests")).ToArray();
            var baseTestFiles = allTestFiles.Where(f => f.Contains("GenericTestExample")).ToArray();
            
            Console.WriteLine($"IntGenericTests files: {intGenericTestFiles.Length}");
            Console.WriteLine($"GenericTestExample files: {baseTestFiles.Length}");
            
            // More precise counting - look for class names in the generated code
            var additionalIntTestFiles = intGenericTestFiles.Where(f => f.Contains("AdditionalIntTest_TestSource")).ToArray();
            var inheritedTestFiles = intGenericTestFiles.Where(f => f.Contains("GenericTest_TestSource")).ToArray();
            
            Console.WriteLine($"AdditionalIntTest classes: {additionalIntTestFiles.Length}");
            Console.WriteLine($"Inherited GenericTest classes: {inheritedTestFiles.Length}");
            
            // This should now show the correct fix: 1 AdditionalIntTest, 1 inherited GenericTest
            await Assert.That(additionalIntTestFiles.Length).IsEqualTo(1);
            await Assert.That(inheritedTestFiles.Length).IsEqualTo(1);
            await Assert.That(intGenericTestFiles.Length).IsEqualTo(2);
        });
}