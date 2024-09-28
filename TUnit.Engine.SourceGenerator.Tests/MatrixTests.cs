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
        Assert.That(generatedFiles[72], Does.Contain("global::TUnit.TestProject.TestEnum methodArg1 = (global::TUnit.TestProject.TestEnum)(-1);"));
        Assert.That(generatedFiles[73], Does.Contain("global::TUnit.TestProject.TestEnum methodArg1 = global::TUnit.TestProject.TestEnum.One;"));
        Assert.That(generatedFiles[74], Does.Contain("global::TUnit.TestProject.TestEnum methodArg1 = (global::TUnit.TestProject.TestEnum)(-1);"));
        Assert.That(generatedFiles[75], Does.Contain("global::TUnit.TestProject.TestEnum methodArg1 = global::TUnit.TestProject.TestEnum.One;"));
    }
}