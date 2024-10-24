using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class BasicTests : TestsBase<TestsGenerator>
{
    [Test]
    public Task Test() => RunTest(Path.Combine(Git.RootDirectory.FullName,
            "TUnit.TestProject",
            "BasicTests.cs"),
        async generatedFiles =>
        {
            await AssertFileContains(generatedFiles[0], "TestId = $\"TUnit.TestProject.BasicTests.SynchronousTest:0\",");
            await AssertFileContains(generatedFiles[0], "TestId = $\"TUnit.TestProject.BasicTests.AsynchronousTest:0\",");
        });
}