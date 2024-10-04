using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class STAThreadHooksTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "STAThreadTests.cs"),
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(2));
            
            AssertFileContains(generatedFiles[0], "TestRegistrar.RegisterBeforeHook");
            AssertFileContains(generatedFiles[0], "HookExecutor = new global::TUnit.Core.STAThreadExecutor(),");
            
            AssertFileContains(generatedFiles[1], "TestRegistrar.RegisterAfterHook");
            AssertFileContains(generatedFiles[1], "HookExecutor = new global::TUnit.Core.STAThreadExecutor(),");
        });
}