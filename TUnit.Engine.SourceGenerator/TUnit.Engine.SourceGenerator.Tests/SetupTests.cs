using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class SetupTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "SetupTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            Assert.That(generatedFiles[0], Does.Contain("TestName = \"TestServerResponse1\","));
            Assert.That(generatedFiles[0], Does.Contain("\t\t\t\tBeforeEachTestSetUps = [classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach1()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach2()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach3()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.Setup()),],"));
            
            Assert.That(generatedFiles[1], Does.Contain("TestName = \"TestServerResponse2\","));
            Assert.That(generatedFiles[1], Does.Contain("\t\t\t\tBeforeEachTestSetUps = [classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach1()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach2()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.BeforeEach3()),classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.Setup()),],"));
        });
}