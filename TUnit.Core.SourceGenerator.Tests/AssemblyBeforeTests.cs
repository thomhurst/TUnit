using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AssemblyBeforeTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "AssemblyBeforeTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);

            await Verify(generatedFiles[0]);
            
            await Verify(generatedFiles[2]);
            
            await Verify(generatedFiles[4]);
            
            await Verify(generatedFiles[6]);
            
            await Verify(generatedFiles[7]);
            
            await Verify(generatedFiles[8]);
            
            await Verify(generatedFiles[9]);
        });
}