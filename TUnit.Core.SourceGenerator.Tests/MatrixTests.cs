using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class MatrixTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MatrixTests.cs"),
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
            AssertTestOne(generatedFiles);
            AssertTestTwo(generatedFiles);
            await AssertTestThree(generatedFiles);
        });

    private void AssertTestOne(string[] generatedFiles)
    {
    }

    private void AssertTestTwo(string[] generatedFiles)
    {
    }

    private async Task AssertTestThree(string[] generatedFiles)
    {
        await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.TestEnum methodArg1 = (global::TUnit.TestProject.TestEnum)(-1);");
        await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.TestEnum methodArg1 = global::TUnit.TestProject.TestEnum.One;");
        await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.TestEnum methodArg1 = (global::TUnit.TestProject.TestEnum)(-1);");
        await AssertFileContains(generatedFiles[0], "global::TUnit.TestProject.TestEnum methodArg1 = global::TUnit.TestProject.TestEnum.One;");
    }
}