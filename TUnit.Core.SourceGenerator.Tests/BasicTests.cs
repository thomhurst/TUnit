using TUnit.Assertions.Assertions.Generics;

namespace TUnit.Core.SourceGenerator.Tests;

internal class BasicTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BasicTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(3);
            
            await AssertFileContains(generatedFiles[0], "TestId = $\"TUnit.TestProject.BasicTests.SynchronousTest:0\",");
            await AssertFileContains(generatedFiles[1], "TestId = $\"TUnit.TestProject.BasicTests.AsynchronousTest:0\",");
            await AssertFileContains(generatedFiles[2], "TestId = $\"TUnit.TestProject.BasicTests.ValueTaskAsynchronousTest:0\",");
        });
}