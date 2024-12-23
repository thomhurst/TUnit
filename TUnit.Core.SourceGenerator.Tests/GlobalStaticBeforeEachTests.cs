using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class GlobalStaticBeforeEachTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BeforeTests",
            "BeforeEveryTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);
            
            await Verify(generatedFiles[7]);
            
            await Verify(generatedFiles[8]);
            
            await Verify(generatedFiles[9]);
            
            await Verify(generatedFiles[10]);
            
            await Verify(generatedFiles[11]);
            
            await Verify(generatedFiles[12]);
            
            await Verify(generatedFiles[13]);
        });
}