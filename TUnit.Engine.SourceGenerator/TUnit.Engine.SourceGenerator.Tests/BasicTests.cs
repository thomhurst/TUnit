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
            
            Assert.That(generatedFiles[0], Does.Contain("TestId = $\"TestAttribute:TUnit.TestProject.BasicTests.SynchronousTest:0\","));
            Assert.That(generatedFiles[1], Does.Contain("TestId = $\"TestAttribute:TUnit.TestProject.BasicTests.AsynchronousTest:0\","));
        });
}