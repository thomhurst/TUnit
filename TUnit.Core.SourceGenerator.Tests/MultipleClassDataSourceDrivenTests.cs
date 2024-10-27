using TUnit.Assertions.Extensions;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class MultipleClassDataSourceDrivenTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "MultipleClassDataSourceDrivenTests.cs"),
        async generatedFiles =>
        {
            await Assert.That(generatedFiles.Length).IsEqualTo(2);

            await AssertFileContains(generatedFiles[0],
                "global::TUnit.TestProject.MultipleClassDataSourceDrivenTests.Inject1 classArg = classArgGeneratedData.Item1;");
            await AssertFileContains(generatedFiles[0],
                "global::TUnit.TestProject.MultipleClassDataSourceDrivenTests.Inject2 classArg1 = classArgGeneratedData.Item2;");
            await AssertFileContains(generatedFiles[0],
                "global::TUnit.TestProject.MultipleClassDataSourceDrivenTests.Inject3 classArg2 = classArgGeneratedData.Item3;");
            await AssertFileContains(generatedFiles[0],
                "global::TUnit.TestProject.MultipleClassDataSourceDrivenTests.Inject4 classArg3 = classArgGeneratedData.Item4;");
            await AssertFileContains(generatedFiles[0],
                "global::TUnit.TestProject.MultipleClassDataSourceDrivenTests.Inject5 classArg4 = classArgGeneratedData.Item5;");
        });
}