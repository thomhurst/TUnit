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
            Assert.That(generatedFiles[0], Does.Contain(
                """
                AfterEachTestCleanUps = [
                new InstanceMethod<global::TUnit.TestProject.CleanUpTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.CleanUpTests).GetMethod("CleanUp", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.CleanUp(), cancellationToken),
                },
                new InstanceMethod<global::TUnit.TestProject.CleanUpTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.CleanUpTests).GetMethod("AfterEach3", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach3(), cancellationToken),
                },
                new InstanceMethod<global::TUnit.TestProject.CleanUpTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.CleanUpTests).GetMethod("AfterEach2", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach2(), cancellationToken),
                },
                new InstanceMethod<global::TUnit.TestProject.CleanUpTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.CleanUpTests).GetMethod("AfterEach1", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach1(), cancellationToken),
                },],
                """));
                
            Assert.That(generatedFiles[1], Does.Contain("TestName = \"Test2\","));
            Assert.That(generatedFiles[1], Does.Contain(
                """
                AfterEachTestCleanUps = [
                new InstanceMethod<global::TUnit.TestProject.CleanUpTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.CleanUpTests).GetMethod("CleanUp", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.CleanUp(), cancellationToken),
                },
                new InstanceMethod<global::TUnit.TestProject.CleanUpTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.CleanUpTests).GetMethod("AfterEach3", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach3(), cancellationToken),
                },
                new InstanceMethod<global::TUnit.TestProject.CleanUpTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.CleanUpTests).GetMethod("AfterEach2", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach2(), cancellationToken),
                },
                new InstanceMethod<global::TUnit.TestProject.CleanUpTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.CleanUpTests).GetMethod("AfterEach1", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach1(), cancellationToken),
                },],
                """
            ));
        });
}