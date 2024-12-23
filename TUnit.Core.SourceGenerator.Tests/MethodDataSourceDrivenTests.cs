using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class MethodDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MethodDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(7);
            
            await Verify(generatedFiles[0]);

            await Verify(generatedFiles[1]);

            await Verify(generatedFiles[2]);
            
            await Verify(generatedFiles[3]);

            await Verify(generatedFiles[4]);
            
            await Verify(generatedFiles[5]);
            
            await Verify(generatedFiles[6]);
        });
}