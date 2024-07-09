using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class BeforeTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));

            Assert.That(generatedFiles[0], Does.Contain("TestName = \"Test1\","));
            Assert.That(generatedFiles[0], Does.Contain(
                """
                				BeforeEachTestSetUps = [
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeEach1", 0, []),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.BeforeEach1(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeEach2", 0, []),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.BeforeEach2(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeEach3", 0, []),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.BeforeEach3(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("Setup", 0, []),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.Setup(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("SetupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.SetupWithContext(testContext), cancellationToken),
                				    },],
                """));

            Assert.That(generatedFiles[1], Does.Contain("TestName = \"Test2\","));
            Assert.That(generatedFiles[1], Does.Contain(
                """
                BeforeEachTestSetUps = [
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeEach1", 0, []),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.BeforeEach1(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeEach2", 0, []),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.BeforeEach2(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("BeforeEach3", 0, []),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.BeforeEach3(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("Setup", 0, []),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.Setup(), cancellationToken),
                				    },
                				    new InstanceMethod<global::TUnit.TestProject.BeforeTests.SetupTests>
                				    {
                    				    MethodInfo = typeof(global::TUnit.TestProject.BeforeTests.SetupTests).GetMethod("SetupWithContext", 0, [typeof(global::TUnit.Core.TestContext)]),
                    				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.SetupWithContext(testContext), cancellationToken),
                				    },],
                """));
        });
}