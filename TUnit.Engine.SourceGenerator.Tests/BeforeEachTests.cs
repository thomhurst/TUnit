using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class BeforeEachTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeEachTests",
            "BeforeEachTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            Assert.That(generatedFiles[0], Does.Contain("TestName = \"Test1\","));
            Assert.That(generatedFiles[0], Does.Contain("\t\t\t\tBeforeEachTestSetUps = [classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach1()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach2()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach3()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.Setup()),],"));
            
            Assert.That(generatedFiles[1], Does.Contain("TestName = \"Test2\","));
            Assert.That(generatedFiles[1], Does.Contain("\t\t\t\tBeforeEachTestSetUps = [classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach1()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach2()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach3()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.Setup()),],"));
        });
}