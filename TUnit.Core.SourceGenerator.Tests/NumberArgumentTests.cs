using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class NumberArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NumberArgumentTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(6);
            
            await Verify(generatedFiles[0]);
            await Verify(generatedFiles[1]);
            await Verify(generatedFiles[2]);
            await Verify(generatedFiles[3]);
            await Verify(generatedFiles[4]);
            await Verify(generatedFiles[5]);
        });

    [Test]
    [SetCulture("de-DE")]
    public Task TestDE() => Test();
}