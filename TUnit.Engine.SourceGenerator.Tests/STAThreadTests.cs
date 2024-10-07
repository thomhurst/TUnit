using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class STAThreadTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "STAThreadTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);
            
            await AssertFileContains(generatedFiles[0], "TestExecutor = new global::TUnit.Core.STAThreadExecutor(),");
        });
}