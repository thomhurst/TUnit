using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class AfterTests : TestsBase<TestHooksGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "AfterTests",
            "AfterTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(14);

            await Verify(generatedFiles[1]);
            
            await Verify(generatedFiles[3]);
            
            await Verify(generatedFiles[5]);
            
            await Verify(generatedFiles[10]);
            
            await Verify(generatedFiles[11]);
            
            await Verify(generatedFiles[12]);
                        
            await Verify(generatedFiles[13]);
        });
}