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
            Assert.That(generatedFiles[0], Does.Contain(
                """
                				BeforeEachTestSetUps = [
                new InstanceMethod<global::TUnit.TestProject.BeforeEachTests.SetupTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeEachTests.SetupTests).GetMethod("BeforeEach1", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.BeforeEach1()),
                },
                new InstanceMethod<global::TUnit.TestProject.BeforeEachTests.SetupTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeEachTests.SetupTests).GetMethod("BeforeEach2", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.BeforeEach2()),
                },
                new InstanceMethod<global::TUnit.TestProject.BeforeEachTests.SetupTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeEachTests.SetupTests).GetMethod("BeforeEach3", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.BeforeEach3()),
                },
                new InstanceMethod<global::TUnit.TestProject.BeforeEachTests.SetupTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeEachTests.SetupTests).GetMethod("Setup", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.Setup()),
                },],
                """));

            Assert.That(generatedFiles[1], Does.Contain("TestName = \"Test2\","));
            Assert.That(generatedFiles[1], Does.Contain(
                """
                BeforeEachTestSetUps = [
                new InstanceMethod<global::TUnit.TestProject.BeforeEachTests.SetupTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeEachTests.SetupTests).GetMethod("BeforeEach1", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.BeforeEach1()),
                },
                new InstanceMethod<global::TUnit.TestProject.BeforeEachTests.SetupTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeEachTests.SetupTests).GetMethod("BeforeEach2", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.BeforeEach2()),
                },
                new InstanceMethod<global::TUnit.TestProject.BeforeEachTests.SetupTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeEachTests.SetupTests).GetMethod("BeforeEach3", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.BeforeEach3()),
                },
                new InstanceMethod<global::TUnit.TestProject.BeforeEachTests.SetupTests>
                {
                    MethodInfo = typeof(global::TUnit.TestProject.BeforeEachTests.SetupTests).GetMethod("Setup", 0, []),
                    Body = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => classInstance.Setup()),
                },],
                """));
        });
}