using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class BasicTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BasicTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            AssertFileContains(generatedFiles[0], "TestId = $\"TUnit.TestProject.BasicTests.SynchronousTest:0\",");
            AssertFileContains(generatedFiles[1], "TestId = $\"TUnit.TestProject.BasicTests.AsynchronousTest:0\",");
        });
}