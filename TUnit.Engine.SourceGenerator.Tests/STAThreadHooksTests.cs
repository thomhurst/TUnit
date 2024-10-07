using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class STAThreadHooksTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "STAThreadTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);
            
            await AssertFileContains(generatedFiles[0], "TestRegistrar.RegisterBeforeHook");
            await AssertFileContains(generatedFiles[0], "HookExecutor = new global::TUnit.Core.STAThreadExecutor(),");
            
            await AssertFileContains(generatedFiles[1], "TestRegistrar.RegisterAfterHook");
            await AssertFileContains(generatedFiles[1], "HookExecutor = new global::TUnit.Core.STAThreadExecutor(),");
        });
}