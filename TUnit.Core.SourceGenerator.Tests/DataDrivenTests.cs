using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class DataDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "DataDrivenTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName,
                    "TUnit.TestProject",
                    "TestEnum.cs")
            ]
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(10);

            await Verify(generatedFiles[0]);

            await Verify(generatedFiles[1]);

            await Verify(generatedFiles[2]);
            
            await Verify(generatedFiles[3]);
            
            await Verify(generatedFiles[4]);
            
            await Verify(generatedFiles[5]);
            
            await Verify(generatedFiles[6]);

            await Verify(generatedFiles[7]);

            await Verify(generatedFiles[8]);

            await Verify(generatedFiles[9]);
        });
}