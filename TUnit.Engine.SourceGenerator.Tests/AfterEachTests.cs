using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class AfterTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AfterTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));

            Assert.That(generatedFiles[0], Does.Contain("TestName = \"Test1\","));
            Assert.That(generatedFiles[0], Does.Contain(
                """
                				AfterEachTestCleanUps = [
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, []),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.Cleanup(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.CleanupWithContext(testContext), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach3", 0, []),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach3(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach2", 0, []),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach2(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach1", 0, []),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach1(), cancellationToken),
                				    },],
                """));

            Assert.That(generatedFiles[1], Does.Contain("TestName = \"Test2\","));
            Assert.That(generatedFiles[1], Does.Contain(
                """
                				AfterEachTestCleanUps = [
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, []),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.Cleanup(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.CleanupWithContext(testContext), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach3", 0, []),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach3(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach2", 0, []),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach2(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach1", 0, []),
                    				    Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach1(), cancellationToken),
                				    },],
                """));
        });
}