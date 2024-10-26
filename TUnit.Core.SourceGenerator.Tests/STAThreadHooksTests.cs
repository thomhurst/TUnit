using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class STAThreadHooksTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "STAThreadTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);
            
            await AssertFileContains(generatedFiles[0], "HookExecutor = new global::TUnit.Core.STAThreadExecutor(),");
            
            await AssertFileContains(generatedFiles[1], "HookExecutor = new global::TUnit.Core.STAThreadExecutor(),");
        });
}