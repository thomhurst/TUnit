using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class CleanUpTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "CleanUpTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            Assert.That(generatedFiles[0], Does.Contain("TestName = \"Test1\","));
            Assert.That(generatedFiles[0], Does.Contain("\t\t\t\tAfterEachTestCleanUps = [classInstance => RunHelpers.RunAsync(() => classInstance.CleanUp()),classInstance => RunHelpers.RunAsync(() => classInstance.AfterEach3()),classInstance => RunHelpers.RunAsync(() => classInstance.AfterEach2()),classInstance => RunHelpers.RunAsync(() => classInstance.AfterEach1()),],"));
            
            Assert.That(generatedFiles[1], Does.Contain("TestName = \"Test2\","));
            Assert.That(generatedFiles[1], Does.Contain("\t\t\t\tAfterEachTestCleanUps = [classInstance => RunHelpers.RunAsync(() => classInstance.CleanUp()),classInstance => RunHelpers.RunAsync(() => classInstance.AfterEach3()),classInstance => RunHelpers.RunAsync(() => classInstance.AfterEach2()),classInstance => RunHelpers.RunAsync(() => classInstance.AfterEach1()),],"));
        });
}