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
            
            await AssertFileContains(generatedFiles[0], "global::System.Int32 methodArg = 1;");
            await AssertFileContains(generatedFiles[1], "global::System.Double methodArg = 1.1;");
            await AssertFileContains(generatedFiles[2], "global::System.Single methodArg = 1.1f;");
            await AssertFileContains(generatedFiles[3], "global::System.Int64 methodArg = 1L;");
            await AssertFileContains(generatedFiles[4], "global::System.UInt64 methodArg = 1UL;");
            await AssertFileContains(generatedFiles[5], "global::System.UInt32 methodArg = 1U;");
        });

    [Test]
    [SetCulture("de-DE")]
    public Task TestDE() => Test();
}