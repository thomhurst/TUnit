using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class TupleDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "TupleDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            
            await Verify(generatedFiles[0]);
            await Verify(generatedFiles[0]);
            await Verify(generatedFiles[0]);
            await Verify(generatedFiles[0]);
            await Verify(generatedFiles[0]);
            await Verify(generatedFiles[0]);
        });
}