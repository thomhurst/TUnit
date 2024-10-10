using TUnit.Assertions.Extensions;
using TUnit.Engine.SourceGenerator.CodeGenerators;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class CustomDisplayNameTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "CustomDisplayNameTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(4);
            
            await AssertFileContains(generatedFiles[0], "DisplayName = $\"A super important test!\",");
            await AssertFileContains(generatedFiles[1], "DisplayName = $\"Another super important test!\",");
            await AssertFileContains(generatedFiles[2], "DisplayName = $\"Test with: {methodArg} {methodArg1} {methodArg2}!\",");
            await AssertFileContains(generatedFiles[3], "DisplayName = $\"Test with: {methodArg} {methodArg1} {methodArg2}!\",");
        });
}