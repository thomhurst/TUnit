using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Tests.Options;

namespace TUnit.Engine.SourceGenerator.Tests;

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
        generatedFiles =>
        {
            Assert.That(generatedFiles.Length, Is.EqualTo(76));

            AssertTestOne(generatedFiles);
            AssertTestTwo(generatedFiles);
            AssertTestThree(generatedFiles);
        });

    private void AssertTestOne(string[] generatedFiles)
    {
    }

    private void AssertTestTwo(string[] generatedFiles)
    {
    }

    private void AssertTestThree(string[] generatedFiles)
    {
        AssertFileContains(generatedFiles[72], "global::TUnit.TestProject.TestEnum methodArg1 = (global::TUnit.TestProject.TestEnum)(-1);");
        AssertFileContains(generatedFiles[73], "global::TUnit.TestProject.TestEnum methodArg1 = global::TUnit.TestProject.TestEnum.One;");
        AssertFileContains(generatedFiles[74], "global::TUnit.TestProject.TestEnum methodArg1 = (global::TUnit.TestProject.TestEnum)(-1);");
        AssertFileContains(generatedFiles[75], "global::TUnit.TestProject.TestEnum methodArg1 = global::TUnit.TestProject.TestEnum.One;");
    }
}