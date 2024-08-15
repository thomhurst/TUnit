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
            
            Assert.That(generatedFiles[0], Does.Contain("TestHookOrchestrator.RegisterBeforeHook"));
            Assert.That(generatedFiles[0], Does.Contain("HookExecutor = new global::TUnit.Core.STAThreadExecutor(),"));
            
            Assert.That(generatedFiles[1], Does.Contain("TestHookOrchestrator.RegisterAfterHook"));
            Assert.That(generatedFiles[1], Does.Contain("HookExecutor = new global::TUnit.Core.STAThreadExecutor(),"));
        });
}