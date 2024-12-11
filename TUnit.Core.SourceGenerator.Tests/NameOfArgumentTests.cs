using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class NameOfArgumentTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "NameOfArgumentTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(1);
            
            await AssertFileContains(generatedFiles[0], "global::System.String methodArg = \"TestName\";");
        });
}