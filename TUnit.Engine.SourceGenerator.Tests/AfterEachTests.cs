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
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.Cleanup(), methodInfo),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.CleanupWithContext(testContext), methodInfo),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach3", 0, []),
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach3(), methodInfo),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach2", 0, []),
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach2(), methodInfo),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach1", 0, []),
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach1(), methodInfo),
                				    },],
                """));

            Assert.That(generatedFiles[1], Does.Contain("TestName = \"Test2\","));
            Assert.That(generatedFiles[1], Does.Contain(
                """
                				AfterEachTestCleanUps = [
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("Cleanup", 0, []),
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.Cleanup(), methodInfo),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("CleanupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.CleanupWithContext(testContext), methodInfo),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach3", 0, []),
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach3(), methodInfo),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach2", 0, []),
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach2(), methodInfo),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.AfterTests.CleanupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.AfterTests.CleanupTests).GetMethod("AfterEach1", 0, []),
                    				    Body = (classInstance, testContext, methodInfo) => RunHelpers.RunWithTimeoutAsync(() => classInstance.AfterEach1(), methodInfo),
                				    },],
                """));
        });
}