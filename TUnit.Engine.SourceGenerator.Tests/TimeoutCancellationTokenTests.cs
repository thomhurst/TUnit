using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class TimeoutCancellationTokenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TimeoutCancellationTokenTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(7));
            
            Assert.That(generatedFiles[0], Does.Contain("TestName = \"BasicTest\""));
            Assert.That(generatedFiles[0], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.BasicTest(cancellationToken))"));
            
            Assert.That(generatedFiles[1], Does.Contain("TestName = \"InheritedTimeoutAttribute\""));
            Assert.That(generatedFiles[1], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.InheritedTimeoutAttribute(cancellationToken))"));

            Assert.That(generatedFiles[2], Does.Contain("TestName = \"DataTest\""));
            Assert.That(generatedFiles[2], Does.Contain("global::System.Int32 methodArg0 = 1;"));
            Assert.That(generatedFiles[2], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.DataTest(methodArg0, cancellationToken))"));
            
            Assert.That(generatedFiles[3], Does.Contain("TestName = \"DataSourceTest\""));
            Assert.That(generatedFiles[3], Does.Contain("global::System.Int32 methodArg0 = global::TUnit.TestProject.TimeoutCancellationTokenTests.DataSource();"));
            Assert.That(generatedFiles[3], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.DataSourceTest(methodArg0, cancellationToken))"));
            
            Assert.That(generatedFiles[4], Does.Contain("TestName = \"CombinativeTest\""));
            Assert.That(generatedFiles[4], Does.Contain("global::System.Int32 methodArg0 = 1;"));
            Assert.That(generatedFiles[4], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.CombinativeTest(methodArg0, cancellationToken))"));
            
            Assert.That(generatedFiles[5], Does.Contain("TestName = \"CombinativeTest\""));
            Assert.That(generatedFiles[5], Does.Contain("global::System.Int32 methodArg0 = 2;"));
            Assert.That(generatedFiles[5], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.CombinativeTest(methodArg0, cancellationToken))"));
            
            Assert.That(generatedFiles[6], Does.Contain("TestName = \"CombinativeTest\""));
            Assert.That(generatedFiles[6], Does.Contain("global::System.Int32 methodArg0 = 3;"));
            Assert.That(generatedFiles[6], Does.Contain("TestMethodFactory = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.CombinativeTest(methodArg0, cancellationToken))"));
        });
}